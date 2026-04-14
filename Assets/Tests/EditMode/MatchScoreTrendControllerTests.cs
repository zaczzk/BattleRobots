using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for T202:
    ///   <see cref="MatchScoreTrendController"/>.
    ///
    /// MatchScoreTrendControllerTests (12):
    ///   FreshInstance_ScoreHistoryIsNull                   ×1
    ///   OnEnable_AllNullRefs_DoesNotThrow                  ×1
    ///   OnDisable_AllNullRefs_DoesNotThrow                 ×1
    ///   OnDisable_Unregisters                              ×1
    ///   Refresh_NullHistory_ShowsDash                      ×1
    ///   Refresh_EmptyHistory_ShowsDash                     ×1
    ///   Refresh_SingleEntry_ShowsDash                      ×1
    ///   Refresh_PositiveTrend_ShowsImproving               ×1
    ///   Refresh_NegativeTrend_ShowsDeclining               ×1
    ///   Refresh_ZeroTrend_TwoEntries_ShowsSteady           ×1
    ///   OnHistoryUpdated_TriggersRefresh                   ×1
    ///   Refresh_NullLabel_DoesNotThrow                     ×1
    ///
    /// Total: 12 new EditMode tests.
    /// </summary>
    public class MatchScoreTrendControllerTests
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

        private static ScoreHistorySO CreateHistory(params int[] scores)
        {
            var so = ScriptableObject.CreateInstance<ScoreHistorySO>();
            foreach (int s in scores) so.Record(s);
            return so;
        }

        private static VoidGameEvent CreateEvent() =>
            ScriptableObject.CreateInstance<VoidGameEvent>();

        private static MatchScoreTrendController CreateController()
        {
            var go = new GameObject("MatchScoreTrendCtrl_Test");
            return go.AddComponent<MatchScoreTrendController>();
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void Ctrl_FreshInstance_ScoreHistoryIsNull()
        {
            var ctrl = CreateController();
            Assert.IsNull(ctrl.ScoreHistory);
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnEnable_AllNullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnEnable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnDisable_AllNullRefs_DoesNotThrow()
        {
            var ctrl = CreateController();
            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable");
            Assert.DoesNotThrow(() => InvokePrivate(ctrl, "OnDisable"));
            Object.DestroyImmediate(ctrl.gameObject);
        }

        [Test]
        public void Ctrl_OnDisable_Unregisters()
        {
            var ctrl = CreateController();
            var ch   = CreateEvent();
            SetField(ctrl, "_onHistoryUpdated", ch);
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
        public void Ctrl_Refresh_NullHistory_ShowsDash()
        {
            var ctrl   = CreateController();
            var labelGO = new GameObject("Label");
            var label   = labelGO.AddComponent<Text>();
            SetField(ctrl, "_trendLabel", label);

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            StringAssert.Contains("\u2014", label.text,
                "Null ScoreHistorySO must show an em-dash.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
        }

        [Test]
        public void Ctrl_Refresh_EmptyHistory_ShowsDash()
        {
            var ctrl    = CreateController();
            var history = ScriptableObject.CreateInstance<ScoreHistorySO>(); // no entries
            var labelGO = new GameObject("Label");
            var label   = labelGO.AddComponent<Text>();

            SetField(ctrl, "_scoreHistory", history);
            SetField(ctrl, "_trendLabel",   label);

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            StringAssert.Contains("\u2014", label.text,
                "Empty history (0 entries) must show an em-dash.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(history);
        }

        [Test]
        public void Ctrl_Refresh_SingleEntry_ShowsDash()
        {
            var ctrl    = CreateController();
            var history = CreateHistory(500); // only 1 entry — no trend computable
            var labelGO = new GameObject("Label");
            var label   = labelGO.AddComponent<Text>();

            SetField(ctrl, "_scoreHistory", history);
            SetField(ctrl, "_trendLabel",   label);

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            StringAssert.Contains("\u2014", label.text,
                "A single-entry history must show an em-dash (insufficient data).");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(history);
        }

        [Test]
        public void Ctrl_Refresh_PositiveTrend_ShowsImproving()
        {
            var ctrl    = CreateController();
            var history = CreateHistory(400, 600); // latest > oldest → improving
            var labelGO = new GameObject("Label");
            var label   = labelGO.AddComponent<Text>();

            SetField(ctrl, "_scoreHistory", history);
            SetField(ctrl, "_trendLabel",   label);

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            StringAssert.Contains("Improving", label.text,
                "Positive TrendDelta must show the 'Improving' label.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(history);
        }

        [Test]
        public void Ctrl_Refresh_NegativeTrend_ShowsDeclining()
        {
            var ctrl    = CreateController();
            var history = CreateHistory(700, 400); // latest < oldest → declining
            var labelGO = new GameObject("Label");
            var label   = labelGO.AddComponent<Text>();

            SetField(ctrl, "_scoreHistory", history);
            SetField(ctrl, "_trendLabel",   label);

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            StringAssert.Contains("Declining", label.text,
                "Negative TrendDelta must show the 'Declining' label.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(history);
        }

        [Test]
        public void Ctrl_Refresh_ZeroTrend_TwoEntries_ShowsSteady()
        {
            var ctrl    = CreateController();
            var history = CreateHistory(500, 500); // same → zero delta → steady
            var labelGO = new GameObject("Label");
            var label   = labelGO.AddComponent<Text>();

            SetField(ctrl, "_scoreHistory", history);
            SetField(ctrl, "_trendLabel",   label);

            InvokePrivate(ctrl, "Awake");
            ctrl.Refresh();

            StringAssert.Contains("Steady", label.text,
                "Zero TrendDelta must show the 'Steady' label.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(history);
        }

        [Test]
        public void Ctrl_OnHistoryUpdated_TriggersRefresh()
        {
            var ctrl    = CreateController();
            var history = CreateHistory(300, 500); // improving initially
            var ch      = CreateEvent();
            var labelGO = new GameObject("Label");
            var label   = labelGO.AddComponent<Text>();

            SetField(ctrl, "_scoreHistory",      history);
            SetField(ctrl, "_onHistoryUpdated", ch);
            SetField(ctrl, "_trendLabel",        label);

            InvokePrivate(ctrl, "Awake");
            InvokePrivate(ctrl, "OnEnable"); // Refresh: improving (200 > 0)

            history.Record(100); // now trend: oldest=300, newest=100 → declining; but also fires internal event
            // Simulate external event raise.
            ch.Raise();

            StringAssert.Contains("Declining", label.text,
                "OnHistoryUpdated must trigger Refresh() to update the trend label.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(labelGO);
            Object.DestroyImmediate(history);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void Ctrl_Refresh_NullLabel_DoesNotThrow()
        {
            var ctrl    = CreateController();
            var history = CreateHistory(400, 600);
            SetField(ctrl, "_scoreHistory", history);
            // _trendLabel intentionally null

            InvokePrivate(ctrl, "Awake");
            Assert.DoesNotThrow(() => ctrl.Refresh(),
                "Refresh() must not throw when _trendLabel is null.");

            Object.DestroyImmediate(ctrl.gameObject);
            Object.DestroyImmediate(history);
        }
    }
}
