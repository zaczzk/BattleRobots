using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T346: <see cref="ZoneControlMatchRecapSO"/> and
    /// <see cref="ZoneControlMatchRecapController"/>.
    ///
    /// ZoneControlMatchRecapTests (12):
    ///   SO_FreshInstance_IsBuilt_False                                       ×1
    ///   SO_FreshInstance_GetRecapSummary_ReturnsEmpty                        ×1
    ///   SO_BuildRecap_NullSOs_SetsIsBuilt_True                               ×1
    ///   SO_BuildRecap_NullSOs_GetRecapSummary_NotEmpty                       ×1
    ///   SO_BuildRecap_FiresOnRecapBuilt                                       ×1
    ///   SO_BuildRecap_WithScoreboard_RecordsScores                           ×1
    ///   SO_Reset_ClearsIsBuilt                                               ×1
    ///   SO_Reset_GetRecapSummary_ReturnsEmpty                                ×1
    ///   Controller_OnEnable_AllNullRefs_DoesNotThrow                         ×1
    ///   Controller_OnDisable_AllNullRefs_DoesNotThrow                        ×1
    ///   Controller_OnDisable_Unregisters_Channels                            ×1
    ///   Controller_HandleMatchEnded_SetsIsBuilt                              ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class ZoneControlMatchRecapTests
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

        private static ZoneControlMatchRecapSO CreateRecapSO()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlMatchRecapSO>();
            so.Reset();
            return so;
        }

        private static ZoneControlScoreboardSO CreateScoreboardSO(int maxBots = 1)
        {
            var so = ScriptableObject.CreateInstance<ZoneControlScoreboardSO>();
            SetField(so, "_maxBots", maxBots);
            so.Reset();
            return so;
        }

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_IsBuilt_False()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlMatchRecapSO>();
            Assert.IsFalse(so.IsBuilt,
                "IsBuilt must be false on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_GetRecapSummary_ReturnsEmpty()
        {
            var so = ScriptableObject.CreateInstance<ZoneControlMatchRecapSO>();
            Assert.AreEqual(string.Empty, so.GetRecapSummary(),
                "GetRecapSummary must return empty string when recap is not built.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BuildRecap_NullSOs_SetsIsBuilt_True()
        {
            var so = CreateRecapSO();
            so.BuildRecap(null, null, null);
            Assert.IsTrue(so.IsBuilt,
                "IsBuilt must be true after BuildRecap even when all SOs are null.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BuildRecap_NullSOs_GetRecapSummary_NotEmpty()
        {
            var so = CreateRecapSO();
            so.BuildRecap(null, null, null);
            Assert.IsNotEmpty(so.GetRecapSummary(),
                "GetRecapSummary must return a non-empty string after BuildRecap.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_BuildRecap_FiresOnRecapBuilt()
        {
            var so  = CreateRecapSO();
            var evt = CreateEvent();
            SetField(so, "_onRecapBuilt", evt);

            int count = 0;
            evt.RegisterCallback(() => count++);

            so.BuildRecap(null, null, null);

            Assert.AreEqual(1, count,
                "_onRecapBuilt must fire once when BuildRecap is called.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void SO_BuildRecap_WithScoreboard_RecordsScores()
        {
            var so          = CreateRecapSO();
            var scoreboardSO = CreateScoreboardSO();

            // Add some player/bot score via RecordCapture methods.
            scoreboardSO.RecordPlayerCapture();  // player score → 1
            scoreboardSO.RecordBotCapture(0);    // bot score → 1

            so.BuildRecap(null, null, scoreboardSO);

            Assert.AreEqual(1, so.FinalPlayerScore,
                "FinalPlayerScore must match the scoreboard player score.");
            Assert.AreEqual(1, so.FinalBotScore,
                "FinalBotScore must match the scoreboard bot score.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(scoreboardSO);
        }

        [Test]
        public void SO_Reset_ClearsIsBuilt()
        {
            var so = CreateRecapSO();
            so.BuildRecap(null, null, null);
            so.Reset();
            Assert.IsFalse(so.IsBuilt,
                "IsBuilt must be false after Reset.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_GetRecapSummary_ReturnsEmpty()
        {
            var so = CreateRecapSO();
            so.BuildRecap(null, null, null);
            so.Reset();
            Assert.AreEqual(string.Empty, so.GetRecapSummary(),
                "GetRecapSummary must return empty after Reset.");
            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_OnEnable_AllNullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_MatchRecap_OnEnable_Null");
            Assert.DoesNotThrow(
                () => go.AddComponent<ZoneControlMatchRecapController>(),
                "Adding controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_AllNullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_MatchRecap_OnDisable_Null");
            go.AddComponent<ZoneControlMatchRecapController>();
            Assert.DoesNotThrow(() => go.SetActive(false),
                "Disabling controller with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channels()
        {
            var go   = new GameObject("Test_MatchRecap_Unregister");
            var ctrl = go.AddComponent<ZoneControlMatchRecapController>();
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
        public void Controller_HandleMatchEnded_SetsIsBuilt()
        {
            var go       = new GameObject("Test_MatchRecap_HandleMatchEnded");
            var ctrl     = go.AddComponent<ZoneControlMatchRecapController>();
            var recapSO  = CreateRecapSO();
            SetField(ctrl, "_recapSO", recapSO);

            ctrl.HandleMatchEnded();

            Assert.IsTrue(recapSO.IsBuilt,
                "HandleMatchEnded must call BuildRecap, setting IsBuilt to true.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(recapSO);
        }
    }
}
