using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T343: <see cref="ZoneControlMVPSO"/> and
    /// <see cref="ZoneControlMVPController"/>.
    ///
    /// ZoneControlMVPTests (12):
    ///   SO_FreshInstance_PlayerCaptures_Zero                              ×1
    ///   SO_RecordPlayerCapture_IncrementsPlayerCaptures                   ×1
    ///   SO_RecordBotCapture_IncrementsBotCaptures                         ×1
    ///   SO_AddPlayerPresenceTime_AccumulatesTime                          ×1
    ///   SO_IsPlayerMVP_PlayerMoreCaptures_ReturnsTrue                     ×1
    ///   SO_IsPlayerMVP_EqualCaptures_PlayerMorePresence_ReturnsTrue       ×1
    ///   SO_IsPlayerMVP_BotMoreCaptures_ReturnsFalse                       ×1
    ///   SO_Reset_ClearsAllFields                                          ×1
    ///   Controller_OnEnable_AllNullRefs_DoesNotThrow                      ×1
    ///   Controller_OnDisable_AllNullRefs_DoesNotThrow                     ×1
    ///   Controller_OnDisable_Unregisters_Channel                          ×1
    ///   Controller_HandleMatchEnded_Refresh_ShowsPanel                    ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlMVPTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static ZoneControlMVPSO CreateMVPSO()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlMVPSO>();
            so.Reset();
            return so;
        }

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_PlayerCaptures_Zero()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlMVPSO>();
            Assert.AreEqual(0, so.PlayerCaptures,
                "PlayerCaptures must be 0 on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordPlayerCapture_IncrementsPlayerCaptures()
        {
            var so = CreateMVPSO();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            Assert.AreEqual(2, so.PlayerCaptures,
                "PlayerCaptures must increment by 1 per RecordPlayerCapture call.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_RecordBotCapture_IncrementsBotCaptures()
        {
            var so = CreateMVPSO();
            so.RecordBotCapture();
            Assert.AreEqual(1, so.BotCaptures,
                "BotCaptures must increment by 1 per RecordBotCapture call.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddPlayerPresenceTime_AccumulatesTime()
        {
            var so = CreateMVPSO();
            so.AddPlayerPresenceTime(5f);
            so.AddPlayerPresenceTime(3f);
            Assert.AreEqual(8f, so.PlayerPresenceTime, 0.001f,
                "PlayerPresenceTime must accumulate across multiple calls.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_IsPlayerMVP_PlayerMoreCaptures_ReturnsTrue()
        {
            var so = CreateMVPSO();
            so.RecordPlayerCapture();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            Assert.IsTrue(so.IsPlayerMVP,
                "IsPlayerMVP must be true when player has more captures than bot.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_IsPlayerMVP_EqualCaptures_PlayerMorePresence_ReturnsTrue()
        {
            var so = CreateMVPSO();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.AddPlayerPresenceTime(10f);
            so.AddBotPresenceTime(5f);
            Assert.IsTrue(so.IsPlayerMVP,
                "IsPlayerMVP must be true when captures are tied but player has more presence time.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_IsPlayerMVP_BotMoreCaptures_ReturnsFalse()
        {
            var so = CreateMVPSO();
            so.RecordBotCapture();
            so.RecordBotCapture();
            Assert.IsFalse(so.IsPlayerMVP,
                "IsPlayerMVP must be false when bot has more captures than player.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ClearsAllFields()
        {
            var so = CreateMVPSO();
            so.RecordPlayerCapture();
            so.RecordBotCapture();
            so.AddPlayerPresenceTime(10f);
            so.AddBotPresenceTime(8f);

            so.Reset();

            Assert.AreEqual(0, so.PlayerCaptures,   "PlayerCaptures must be 0 after Reset.");
            Assert.AreEqual(0, so.BotCaptures,      "BotCaptures must be 0 after Reset.");
            Assert.AreEqual(0f, so.PlayerPresenceTime, 0.001f, "PlayerPresenceTime must be 0 after Reset.");
            Assert.AreEqual(0f, so.BotPresenceTime,    0.001f, "BotPresenceTime must be 0 after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_OnEnable_AllNullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_MVP_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlMVPController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_AllNullRefs_DoesNotThrow()
        {
            var go   = new GameObject("Test_MVP_OnDisable_Null");
            var ctrl = go.AddComponent<ZoneControlMVPController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_MVP_Unregister");
            var ctrl = go.AddComponent<ZoneControlMVPController>();
            var evt  = CreateEvent();
            SetField(ctrl, "_onMatchEnded", evt);

            go.SetActive(true);
            go.SetActive(false);

            int count = 0;
            evt.RegisterCallback(() => count++);
            evt.Raise();

            Assert.AreEqual(1, count,
                "_onMatchEnded must be unregistered after OnDisable.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_HandleMatchEnded_Refresh_ShowsPanel()
        {
            var go    = new GameObject("Test_MVP_ShowsPanel");
            var ctrl  = go.AddComponent<ZoneControlMVPController>();
            var mvpSO = CreateMVPSO();
            var panel = new GameObject("Panel");

            SetField(ctrl, "_mvpSO",    mvpSO);
            SetField(ctrl, "_mvpPanel", panel);

            ctrl.HandleMatchEnded();

            Assert.IsTrue(panel.activeSelf,
                "_mvpPanel must be active after HandleMatchEnded when MVPSO is assigned.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(mvpSO);
        }
    }
}
