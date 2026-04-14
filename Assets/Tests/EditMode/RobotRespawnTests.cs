using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T228: <see cref="RobotRespawnSO"/> and
    /// <see cref="RespawnCountdownController"/>.
    ///
    /// RobotRespawnSOTests (10):
    ///   FreshInstance_MaxRespawns_Is3                            ×1
    ///   FreshInstance_RespawnCooldown_Is5                        ×1
    ///   FreshInstance_RespawnsRemaining_Is3                      ×1
    ///   FreshInstance_IsOnCooldown_False                         ×1
    ///   RequestRespawn_Success_DecreasesRemaining                ×1
    ///   RequestRespawn_Success_SetsOnCooldown                    ×1
    ///   RequestRespawn_AllUsed_ReturnsFalse                      ×1
    ///   RequestRespawn_FiresOnRespawnUsed                        ×1
    ///   Tick_ExpiringCooldown_FiresOnRespawnReady                ×1
    ///   Reset_ClearsRespawnsUsedAndCooldown                      ×1
    ///
    /// RespawnCountdownControllerTests (6):
    ///   FreshInstance_RespawnSONull                              ×1
    ///   OnEnable_NullRefs_DoesNotThrow                           ×1
    ///   OnDisable_Unregisters                                    ×1
    ///   Refresh_NullSO_HidesPanel                                ×1
    ///   Refresh_WithSO_ShowsPanel                                ×1
    ///   Refresh_NullPanel_DoesNotThrow                           ×1
    ///
    /// Total: 16 new EditMode tests.
    /// </summary>
    public class RobotRespawnSOTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static void InvokeMethod(object target, string name)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{name}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        private static RobotRespawnSO MakeSO()
        {
            var so = ScriptableObject.CreateInstance<RobotRespawnSO>();
            InvokeMethod(so, "OnEnable"); // initialise runtime state
            return so;
        }

        private static VoidGameEvent MakeEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        // ── Fresh-instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_MaxRespawns_Is3()
        {
            var so = MakeSO();
            Assert.AreEqual(3, so.MaxRespawns,
                "Default MaxRespawns must be 3.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_RespawnCooldown_Is5()
        {
            var so = MakeSO();
            Assert.AreEqual(5f, so.RespawnCooldown, 0.001f,
                "Default RespawnCooldown must be 5 seconds.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_RespawnsRemaining_Is3()
        {
            var so = MakeSO();
            Assert.AreEqual(3, so.RespawnsRemaining,
                "Fresh instance must have all 3 respawns remaining.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void FreshInstance_IsOnCooldown_False()
        {
            var so = MakeSO();
            Assert.IsFalse(so.IsOnCooldown,
                "Fresh instance must not be on cooldown.");
            Object.DestroyImmediate(so);
        }

        // ── RequestRespawn ────────────────────────────────────────────────────

        [Test]
        public void RequestRespawn_Success_DecreasesRemaining()
        {
            var so = MakeSO();
            bool result = so.RequestRespawn();
            Assert.IsTrue(result, "RequestRespawn must return true when slots are available.");
            Assert.AreEqual(2, so.RespawnsRemaining,
                "RespawnsRemaining must decrease by 1 after a successful request.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void RequestRespawn_Success_SetsOnCooldown()
        {
            var so = MakeSO();
            so.RequestRespawn();
            Assert.IsTrue(so.IsOnCooldown,
                "IsOnCooldown must be true immediately after RequestRespawn.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void RequestRespawn_AllUsed_ReturnsFalse()
        {
            var so = MakeSO();
            so.RequestRespawn();
            so.RequestRespawn();
            so.RequestRespawn();
            bool result = so.RequestRespawn(); // 4th request — should be rejected
            Assert.IsFalse(result, "RequestRespawn must return false when all slots are exhausted.");
            Assert.AreEqual(0, so.RespawnsRemaining,
                "RespawnsRemaining must not go below 0.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void RequestRespawn_FiresOnRespawnUsed()
        {
            var so  = MakeSO();
            var evt = MakeEvent();
            SetField(so, "_onRespawnUsed", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);

            so.RequestRespawn();

            Assert.AreEqual(1, fired,
                "_onRespawnUsed must be raised once by a successful RequestRespawn.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        // ── Tick & cooldown expiry ─────────────────────────────────────────────

        [Test]
        public void Tick_ExpiringCooldown_FiresOnRespawnReady()
        {
            var so  = MakeSO();
            var evt = MakeEvent();
            SetField(so, "_onRespawnReady", evt);

            int fired = 0;
            evt.RegisterCallback(() => fired++);

            so.RequestRespawn();                  // starts cooldown (5 s)
            so.Tick(so.RespawnCooldown + 0.01f);  // expire cooldown in one tick

            Assert.AreEqual(1, fired,
                "_onRespawnReady must be raised exactly once when the cooldown expires.");
            Assert.IsFalse(so.IsOnCooldown,
                "IsOnCooldown must be false after cooldown expires.");
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void Reset_ClearsRespawnsUsedAndCooldown()
        {
            var so = MakeSO();
            so.RequestRespawn();
            so.RequestRespawn();
            so.Reset();

            Assert.AreEqual(3, so.RespawnsRemaining,
                "RespawnsRemaining must be restored to MaxRespawns after Reset.");
            Assert.IsFalse(so.IsOnCooldown,
                "IsOnCooldown must be false after Reset.");
            Object.DestroyImmediate(so);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // RespawnCountdownController tests
    // ═══════════════════════════════════════════════════════════════════════════

    public class RespawnCountdownControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static RespawnCountdownController MakeController(out GameObject go)
        {
            go = new GameObject("RespawnCountdownCtrl_Test");
            go.SetActive(false);
            return go.AddComponent<RespawnCountdownController>();
        }

        private static RobotRespawnSO MakeSO()
        {
            var so = ScriptableObject.CreateInstance<RobotRespawnSO>();
            // Invoke OnEnable to initialise runtime state.
            typeof(RobotRespawnSO)
                .GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic)?
                .Invoke(so, null);
            return so;
        }

        private static VoidGameEvent MakeEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        // ── Fresh-instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_RespawnSONull()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<RespawnCountdownController>();
            Assert.IsNull(ctrl.RespawnSO,
                "Fresh RespawnCountdownController must have null RespawnSO.");
            Object.DestroyImmediate(go);
        }

        // ── OnEnable / OnDisable ──────────────────────────────────────────────

        [Test]
        public void OnEnable_NullRefs_DoesNotThrow()
        {
            MakeController(out GameObject go);
            Assert.DoesNotThrow(() => go.SetActive(true),
                "OnEnable with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_Unregisters()
        {
            var channel = MakeEvent();
            int external = 0;
            channel.RegisterCallback(() => external++);

            MakeController(out GameObject go);
            var ctrl = go.GetComponent<RespawnCountdownController>();
            SetField(ctrl, "_onRespawnUsed", channel);

            go.SetActive(true);   // Awake + OnEnable
            go.SetActive(false);  // OnDisable — must unsubscribe

            channel.Raise();

            Assert.AreEqual(1, external,
                "After OnDisable, only the external callback should fire.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);
        }

        // ── Refresh ───────────────────────────────────────────────────────────

        [Test]
        public void Refresh_NullSO_HidesPanel()
        {
            MakeController(out GameObject go);
            var ctrl  = go.GetComponent<RespawnCountdownController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);
            SetField(ctrl, "_panel", panel);
            // _respawnSO remains null

            go.SetActive(true); // triggers OnEnable → Refresh()

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when _respawnSO is null.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_WithSO_ShowsPanel()
        {
            MakeController(out GameObject go);
            var ctrl  = go.GetComponent<RespawnCountdownController>();
            var so    = MakeSO();
            var panel = new GameObject("Panel");
            panel.SetActive(false);
            SetField(ctrl, "_respawnSO", so);
            SetField(ctrl, "_panel",     panel);

            go.SetActive(true); // triggers OnEnable → Refresh()

            Assert.IsTrue(panel.activeSelf,
                "Panel must be shown when a valid _respawnSO is assigned.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Refresh_NullPanel_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<RespawnCountdownController>();
            var so   = MakeSO();
            SetField(ctrl, "_respawnSO", so);
            // _panel remains null

            Assert.DoesNotThrow(() => go.SetActive(true),
                "Refresh with null _panel must not throw.");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(so);
        }
    }
}
