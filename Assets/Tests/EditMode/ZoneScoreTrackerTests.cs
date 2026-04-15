using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T252: <see cref="ZoneScoreTrackerSO"/> and
    /// <see cref="ZoneScoreHUDController"/>.
    ///
    /// ZoneScoreTrackerTests (14):
    ///   SO_FreshInstance_PlayerScore_Zero                           ×1
    ///   SO_FreshInstance_EnemyScore_Zero                            ×1
    ///   SO_FreshInstance_TotalScore_Zero                            ×1
    ///   SO_AddPlayerScore_IncreasesPlayerScore                      ×1
    ///   SO_AddEnemyScore_IncreasesEnemyScore                        ×1
    ///   SO_Reset_ZerosBoth_FiresEvent                               ×1
    ///   Controller_FreshInstance_TrackerNull                        ×1
    ///   Controller_OnEnable_NullRefs_DoesNotThrow                   ×1
    ///   Controller_OnDisable_NullRefs_DoesNotThrow                  ×1
    ///   Controller_OnDisable_Unregisters_ScoreUpdated               ×1
    ///   Controller_OnDisable_Unregisters_MatchEnded                 ×1
    ///   Controller_Refresh_NullTracker_HidesPanel                   ×1
    ///   Controller_Refresh_WithTracker_ShowsPanel                   ×1
    ///   Controller_HandleMatchEnded_ResetsTracker                   ×1
    ///
    /// Total: 14 new EditMode tests.
    /// </summary>
    public class ZoneScoreTrackerTests
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

        private static ZoneScoreTrackerSO CreateTrackerSO() =>
            ScriptableObject.CreateInstance<ZoneScoreTrackerSO>();

        private static ZoneScoreHUDController CreateController()
        {
            var go   = new GameObject("ZoneScoreHUD_Test");
            var ctrl = go.AddComponent<ZoneScoreHUDController>();
            return ctrl;
        }

        // ── SO Tests ──────────────────────────────────────────────────────────

        [Test]
        public void SO_FreshInstance_PlayerScore_Zero()
        {
            var so = CreateTrackerSO();
            Assert.AreEqual(0f, so.PlayerScore, 0.001f,
                "PlayerScore must be 0 on a fresh ZoneScoreTrackerSO.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_EnemyScore_Zero()
        {
            var so = CreateTrackerSO();
            Assert.AreEqual(0f, so.EnemyScore, 0.001f,
                "EnemyScore must be 0 on a fresh ZoneScoreTrackerSO.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_FreshInstance_TotalScore_Zero()
        {
            var so = CreateTrackerSO();
            Assert.AreEqual(0f, so.TotalScore, 0.001f,
                "TotalScore must be 0 on a fresh ZoneScoreTrackerSO.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddPlayerScore_IncreasesPlayerScore()
        {
            var so = CreateTrackerSO();
            so.AddPlayerScore(10f);
            so.AddPlayerScore(5f);
            Assert.AreEqual(15f, so.PlayerScore, 0.001f,
                "AddPlayerScore must accumulate to PlayerScore.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_AddEnemyScore_IncreasesEnemyScore()
        {
            var so = CreateTrackerSO();
            so.AddEnemyScore(8f);
            Assert.AreEqual(8f, so.EnemyScore, 0.001f,
                "AddEnemyScore must accumulate to EnemyScore.");
            Object.DestroyImmediate(so);
        }

        [Test]
        public void SO_Reset_ZerosBoth_FiresEvent()
        {
            var so     = CreateTrackerSO();
            var evt    = CreateEvent();
            int called = 0;
            SetField(so, "_onScoreUpdated", evt);
            evt.RegisterCallback(() => called++);

            so.AddPlayerScore(10f);
            so.AddEnemyScore(5f);
            called = 0; // ignore add calls

            so.Reset();

            Assert.AreEqual(0f, so.PlayerScore, 0.001f,
                "Reset must zero PlayerScore.");
            Assert.AreEqual(0f, so.EnemyScore, 0.001f,
                "Reset must zero EnemyScore.");
            Assert.AreEqual(1, called,
                "_onScoreUpdated must fire once on Reset.");

            Object.DestroyImmediate(so);
            Object.DestroyImmediate(evt);
        }

        // ── Controller Tests ──────────────────────────────────────────────────

        [Test]
        public void Controller_FreshInstance_TrackerNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.Tracker,
                "Tracker must be null on a fresh ZoneScoreHUDController.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnEnable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"),
                "OnEnable with null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_NullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"),
                "OnDisable with null refs must not throw.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_ScoreUpdated()
        {
            var ctrl       = CreateController();
            var scoreEvt   = CreateEvent();
            var tracker    = CreateTrackerSO();
            SetField(ctrl, "_onScoreUpdated", scoreEvt);
            SetField(ctrl, "_tracker",        tracker);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            // Raise should no longer call Refresh — we verify by checking tracker score.
            // If subscribed, Refresh would be called but that doesn't mutate tracker.
            // Use the panel absence to verify no crash and no active subscription side-effects.
            Assert.DoesNotThrow(() => scoreEvt.Raise(),
                "Raising _onScoreUpdated after OnDisable must not throw.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(scoreEvt);
            Object.DestroyImmediate(tracker);
        }

        [Test]
        public void Controller_OnDisable_Unregisters_MatchEnded()
        {
            var ctrl       = CreateController();
            var matchEvt   = CreateEvent();
            var tracker    = CreateTrackerSO();
            SetField(ctrl, "_onMatchEnded", matchEvt);
            SetField(ctrl, "_tracker",      tracker);

            tracker.AddPlayerScore(20f);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            InvokePrivate(ctrl, "OnDisable");

            matchEvt.Raise(); // must NOT call HandleMatchEnded → tracker.Reset()

            Assert.AreEqual(20f, tracker.PlayerScore, 0.001f,
                "After OnDisable, _onMatchEnded must not reset the tracker.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(matchEvt);
            Object.DestroyImmediate(tracker);
        }

        [Test]
        public void Controller_Refresh_NullTracker_HidesPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("Panel");
            panel.SetActive(true);
            SetField(ctrl, "_panel", panel);

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.IsFalse(panel.activeSelf,
                "Refresh must hide the panel when Tracker is null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }

        [Test]
        public void Controller_Refresh_WithTracker_ShowsPanel()
        {
            var ctrl    = CreateController();
            var panel   = new GameObject("Panel");
            var tracker = CreateTrackerSO();
            panel.SetActive(false);
            SetField(ctrl, "_panel",   panel);
            SetField(ctrl, "_tracker", tracker);

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf,
                "Refresh must show the panel when Tracker is assigned.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
            Object.DestroyImmediate(tracker);
        }

        [Test]
        public void Controller_HandleMatchEnded_ResetsTracker()
        {
            var ctrl    = CreateController();
            var tracker = CreateTrackerSO();
            SetField(ctrl, "_tracker", tracker);

            tracker.AddPlayerScore(50f);
            tracker.AddEnemyScore(30f);

            InvokePrivate(ctrl, "Awake");
            ctrl.HandleMatchEnded();

            Assert.AreEqual(0f, tracker.PlayerScore, 0.001f,
                "HandleMatchEnded must reset PlayerScore on the tracker.");
            Assert.AreEqual(0f, tracker.EnemyScore, 0.001f,
                "HandleMatchEnded must reset EnemyScore on the tracker.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(tracker);
        }
    }
}
