using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T204:
    ///   <see cref="MatchStreakSummaryController"/>.
    ///
    /// MatchStreakSummaryControllerTests (12):
    ///   FreshInstance_WinStreakIsNull                      ×1
    ///   FreshInstance_MatchRatingIsNull                    ×1
    ///   FreshInstance_ScoreHistoryIsNull                   ×1
    ///   OnEnable_AllNullRefs_DoesNotThrow                  ×1
    ///   OnDisable_AllNullRefs_DoesNotThrow                 ×1
    ///   OnDisable_Unregisters                              ×1
    ///   Refresh_NullWinStreak_ShowsZeroStreak              ×1
    ///   Refresh_WithWinStreak_ShowsCurrentStreak           ×1
    ///   Refresh_WithWinStreak_ShowsBestStreak              ×1
    ///   Refresh_WithMatchRating_ShowsStars                 ×1
    ///   Refresh_NullScoreHistory_ShowsDash                 ×1
    ///   Refresh_ActivatesSummaryPanel                      ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class MatchStreakSummaryControllerTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static void InvokePrivate(object target, string method)
        {
            MethodInfo mi = target.GetType()
                .GetMethod(method,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(mi, $"Method '{method}' not found on {target.GetType().Name}.");
            mi.Invoke(target, null);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static WinStreakSO CreateStreak(int wins, int losses = 0)
        {
            var so = ScriptableObject.CreateInstance<WinStreakSO>();
            for (int i = 0; i < wins;   i++) so.RecordWin();
            for (int i = 0; i < losses; i++) so.RecordLoss();
            return so;
        }

        private static MatchStreakSummaryController CreateController()
        {
            var go = new GameObject("MatchStreakSummary_Test");
            return go.AddComponent<MatchStreakSummaryController>();
        }

        private static Text AddText(GameObject go, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(go.transform);
            return child.AddComponent<Text>();
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_WinStreakIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.WinStreak);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_MatchRatingIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.MatchRating);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void FreshInstance_ScoreHistoryIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.ScoreHistory);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void OnDisable_Unregisters()
        {
            var ctrl = CreateController();
            var ch   = CreateEvent();
            SetField(ctrl, "_onMatchEnded", ch);
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");

            int callCount = 0;
            ch.RegisterCallback(() => callCount++);
            InvokePrivate(ctrl, "OnDisable");
            ch.Raise();

            Assert.AreEqual(1, callCount,
                "After OnDisable only the manually registered callback should fire.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Refresh_NullWinStreak_ShowsZeroStreak()
        {
            var ctrl = CreateController();
            var txt  = AddText(ctrl.gameObject, "streak");
            SetField(ctrl, "_currentStreakText", txt);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual("Streak: 0", txt.text);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Refresh_WithWinStreak_ShowsCurrentStreak()
        {
            var ctrl   = CreateController();
            var streak = CreateStreak(wins: 3);
            var txt    = AddText(ctrl.gameObject, "streak");
            SetField(ctrl, "_winStreak",          streak);
            SetField(ctrl, "_currentStreakText",   txt);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual("Streak: 3", txt.text);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(streak);
        }

        [Test]
        public void Refresh_WithWinStreak_ShowsBestStreak()
        {
            var ctrl   = CreateController();
            // Win 3, lose 1, win 1 → current=1, best=3
            var streak = CreateStreak(wins: 3);
            streak.RecordLoss();
            streak.RecordWin();
            var txt = AddText(ctrl.gameObject, "best");
            SetField(ctrl, "_winStreak",      streak);
            SetField(ctrl, "_bestStreakText",  txt);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual("Best: 3", txt.text);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(streak);
        }

        [Test]
        public void Refresh_WithMatchRating_ShowsStars()
        {
            var ctrl       = CreateController();
            var ratingGo   = new GameObject("Rating");
            var rating     = ratingGo.AddComponent<MatchRatingController>();
            var txt        = AddText(ctrl.gameObject, "stars");
            SetField(ctrl, "_matchRating", rating);
            SetField(ctrl, "_starsText",   txt);
            InvokePrivate(ctrl, "Awake");

            // CurrentStars defaults to 0 on a fresh controller with no match run.
            ctrl.Refresh();

            Assert.AreEqual("0 / 5 \u2605", txt.text);
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(ratingGo);
        }

        [Test]
        public void Refresh_NullScoreHistory_ShowsDash()
        {
            var ctrl = CreateController();
            var txt  = AddText(ctrl.gameObject, "trend");
            SetField(ctrl, "_trendLabel", txt);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.AreEqual("\u2014", txt.text,
                "Null ScoreHistory should produce an em-dash trend label.");
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Refresh_ActivatesSummaryPanel()
        {
            var ctrl  = CreateController();
            var panel = new GameObject("panel");
            panel.SetActive(false);
            SetField(ctrl, "_summaryPanel", panel);
            InvokePrivate(ctrl, "Awake");

            ctrl.Refresh();

            Assert.IsTrue(panel.activeSelf,
                "Refresh() must activate the summary panel.");
            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(panel);
        }
    }
}
