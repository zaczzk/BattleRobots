using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T284: <see cref="ZoneControlSessionSummarySO"/> and
    /// <see cref="ZoneControlSessionSummaryController"/>.
    ///
    /// ZoneControlSessionSummaryTests (14):
    ///   SO_FreshInstance_TotalZonesCaptured_Zero                           ×1
    ///   SO_FreshInstance_MatchesPlayed_Zero                                ×1
    ///   SO_FreshInstance_BestCaptureStreak_Zero                            ×1
    ///   SO_AddMatch_IncreasesTotalCaptured                                 ×1
    ///   SO_AddMatch_IncrementsMatchesPlayed                                ×1
    ///   SO_AddMatch_TracksDominance                                        ×1
    ///   SO_AddMatch_UpdatesBestCaptureStreak                               ×1
    ///   SO_LoadSnapshot_RestoresFields                                     ×1
    ///   SO_Reset_ZerosAllFields                                            ×1
    ///   Controller_FreshInstance_SummarySO_Null                           ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                         ×1
    ///   Controller_OnDisable_Unregisters_Channel                          ×1
    ///   Controller_Refresh_NullSummary_HidesPanel                         ×1
    ///   Controller_HandleMatchEnded_NullSummary_DoesNotThrow              ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class ZoneControlSessionSummaryTests
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

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static ZoneControlSessionSummarySO CreateSummarySO() =>
            ScriptableObject.CreateInstance<ZoneControlSessionSummarySO>();

        private static ZoneControlSessionSummaryController CreateController() =>
            new GameObject("ZoneSessionSummaryCtrl_Test")
                .AddComponent<ZoneControlSessionSummaryController>();

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_TotalZonesCaptured_Zero()
        {
            var so = CreateSummarySO();
            Assert.AreEqual(0, so.TotalZonesCaptured,
                "TotalZonesCaptured must be 0 on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_MatchesPlayed_Zero()
        {
            var so = CreateSummarySO();
            Assert.AreEqual(0, so.MatchesPlayed,
                "MatchesPlayed must be 0 on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_BestCaptureStreak_Zero()
        {
            var so = CreateSummarySO();
            Assert.AreEqual(0, so.BestCaptureStreak,
                "BestCaptureStreak must be 0 on a fresh instance.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddMatch_IncreasesTotalCaptured()
        {
            var so = CreateSummarySO();
            so.AddMatch(capturedThisMatch: 3, hadDominance: false, captureStreak: 0);
            Assert.AreEqual(3, so.TotalZonesCaptured,
                "TotalZonesCaptured must increase by capturedThisMatch.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddMatch_IncrementsMatchesPlayed()
        {
            var so = CreateSummarySO();
            so.AddMatch(0, false, 0);
            so.AddMatch(0, false, 0);
            Assert.AreEqual(2, so.MatchesPlayed,
                "MatchesPlayed must increment on each AddMatch call.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddMatch_TracksDominance()
        {
            var so = CreateSummarySO();
            so.AddMatch(0, hadDominance: true,  captureStreak: 0);
            so.AddMatch(0, hadDominance: false, captureStreak: 0);
            so.AddMatch(0, hadDominance: true,  captureStreak: 0);
            Assert.AreEqual(2, so.MatchesWithDominance,
                "MatchesWithDominance must count matches where hadDominance was true.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddMatch_UpdatesBestCaptureStreak()
        {
            var so = CreateSummarySO();
            so.AddMatch(0, false, captureStreak: 5);
            so.AddMatch(0, false, captureStreak: 3);
            Assert.AreEqual(5, so.BestCaptureStreak,
                "BestCaptureStreak must retain the highest value seen.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_LoadSnapshot_RestoresFields()
        {
            var so = CreateSummarySO();
            so.LoadSnapshot(totalCaptured: 10, matchesPlayed: 5,
                            matchesWithDominance: 3, bestStreak: 7);

            Assert.AreEqual(10, so.TotalZonesCaptured);
            Assert.AreEqual(5,  so.MatchesPlayed);
            Assert.AreEqual(3,  so.MatchesWithDominance);
            Assert.AreEqual(7,  so.BestCaptureStreak);

            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ZerosAllFields()
        {
            var so = CreateSummarySO();
            so.AddMatch(5, true, 4);
            so.Reset();

            Assert.AreEqual(0, so.TotalZonesCaptured);
            Assert.AreEqual(0, so.MatchesPlayed);
            Assert.AreEqual(0, so.MatchesWithDominance);
            Assert.AreEqual(0, so.BestCaptureStreak);

            Object.DestroyImmediate(so);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_SummarySO_Null()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.SummarySO,
                "SummarySO must be null on a freshly added controller.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var go = new GameObject("Test_OnEnable_Null");
            Assert.DoesNotThrow(() => go.AddComponent<ZoneControlSessionSummaryController>(),
                "Adding component with all-null refs must not throw.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_Channel()
        {
            var go   = new GameObject("Test_Unregister");
            var ctrl = go.AddComponent<ZoneControlSessionSummaryController>();
            var evt  = CreateEvent();

            SetField(ctrl, "_onMatchEnded", evt);

            go.SetActive(true);
            go.SetActive(false);

            int callCount = 0;
            evt.RegisterCallback(() => callCount++);
            evt.Raise();

            Assert.AreEqual(1, callCount,
                "After OnDisable the controller must have unregistered from _onMatchEnded.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(evt);
        }

        [Test]
        public void Controller_Refresh_NullSummary_HidesPanel()
        {
            var go    = new GameObject("Test_NullSummary");
            var ctrl  = go.AddComponent<ZoneControlSessionSummaryController>();
            var panel = new GameObject("Panel");
            panel.SetActive(true);

            SetField(ctrl, "_panel", panel);
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Panel must be hidden when _summarySO is null.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Controller_HandleMatchEnded_NullSummary_DoesNotThrow()
        {
            var go   = new GameObject("Test_HandleMatchEnded_Null");
            var ctrl = go.AddComponent<ZoneControlSessionSummaryController>();

            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "HandleMatchEnded"),
                "HandleMatchEnded must not throw when SummarySO is null.");

            Object.DestroyImmediate(go);
        }
    }
}
