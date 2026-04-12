using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="ScoreHistoryController"/>.
    ///
    /// Covers:
    ///   • OnEnable / OnDisable with all-null inspector refs → no throw.
    ///   • OnEnable / OnDisable with null event channel → no throw.
    ///   • OnDisable unregisters the refresh delegate from _onHistoryUpdated.
    ///   • Refresh() with null _scoreHistory → does not throw (sets fallback text).
    ///   • Refresh() with null _listContainer → does not throw (summary labels still updated).
    ///   • FormatTrend: positive delta → "+N ↑" string; negative → "-N ↓"; zero → "±0".
    ///
    /// All tests run headless (no Unity Editor scene required).
    /// Reflection is used to inject private serialized fields.
    /// </summary>
    public class ScoreHistoryControllerTests
    {
        // ── Reflection helpers ────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private static MethodInfo GetMethod(Type type, string name,
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static)
        {
            MethodInfo mi = type.GetMethod(name, flags);
            Assert.IsNotNull(mi, $"Method '{name}' not found on {type.Name}.");
            return mi;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>Activates a GO so Awake / OnEnable fire, then deactivates for OnDisable.</summary>
        private static ScoreHistoryController MakeController(out GameObject go)
        {
            go = new GameObject("ScoreHistoryControllerTest");
            go.SetActive(false);
            var ctrl = go.AddComponent<ScoreHistoryController>();
            return ctrl;
        }

        // ── OnEnable / OnDisable — null guards ────────────────────────────────

        [Test]
        public void OnEnable_AllNullRefs_DoesNotThrow()
        {
            MakeController(out GameObject go);
            Assert.DoesNotThrow(() => go.SetActive(true));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnDisable_AllNullRefs_DoesNotThrow()
        {
            MakeController(out GameObject go);
            go.SetActive(true);
            Assert.DoesNotThrow(() => go.SetActive(false));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void OnEnable_NullChannel_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var history = ScriptableObject.CreateInstance<ScoreHistorySO>();
            var ctrl    = go.GetComponent<ScoreHistoryController>();
            SetField(ctrl, "_scoreHistory", history);
            // _onHistoryUpdated remains null
            Assert.DoesNotThrow(() => go.SetActive(true));
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(history);
        }

        [Test]
        public void OnDisable_NullChannel_DoesNotThrow()
        {
            MakeController(out GameObject go);
            go.SetActive(true);
            Assert.DoesNotThrow(() => go.SetActive(false));
            Object.DestroyImmediate(go);
        }

        // ── OnDisable unregisters ─────────────────────────────────────────────

        [Test]
        public void OnDisable_UnregistersFromOnHistoryUpdated()
        {
            // Set up an external counter to verify no callback fires after OnDisable.
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            int externalCount = 0;
            channel.RegisterCallback(() => externalCount++);

            MakeController(out GameObject go);
            var ctrl = go.GetComponent<ScoreHistoryController>();
            SetField(ctrl, "_onHistoryUpdated", channel);

            go.SetActive(true);   // Awake + OnEnable → controller subscribed
            go.SetActive(false);  // OnDisable → controller must unsubscribe

            // Fire the channel — only the external counter should respond.
            channel.Raise();

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);

            Assert.AreEqual(1, externalCount,
                "Only the external counter should fire; controller must be unsubscribed.");
        }

        // ── Refresh — null guards ─────────────────────────────────────────────

        [Test]
        public void Refresh_NullScoreHistory_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<ScoreHistoryController>();
            go.SetActive(true);

            // _scoreHistory is null — Refresh() must handle gracefully.
            Assert.DoesNotThrow(() => ctrl.Refresh());
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Refresh_NullListContainer_DoesNotThrow()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<ScoreHistoryController>();
            var history = ScriptableObject.CreateInstance<ScoreHistorySO>();
            history.Record(100);
            SetField(ctrl, "_scoreHistory", history);
            // _listContainer remains null
            go.SetActive(true);

            Assert.DoesNotThrow(() => ctrl.Refresh());

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(history);
        }

        // ── FormatTrend ───────────────────────────────────────────────────────

        [Test]
        public void FormatTrend_ZeroDelta_ReturnsSymbol()
        {
            MethodInfo m = GetMethod(typeof(ScoreHistoryController), "FormatTrend");
            string result = (string)m.Invoke(null, new object[] { 0 });
            // Should contain the ± character (Unicode \u00b1).
            StringAssert.Contains("\u00b1", result,
                "Zero delta must produce a '±0' trend string.");
        }

        [Test]
        public void FormatTrend_PositiveDelta_ContainsPlusAndUpArrow()
        {
            MethodInfo m = GetMethod(typeof(ScoreHistoryController), "FormatTrend");
            string result = (string)m.Invoke(null, new object[] { 150 });
            StringAssert.Contains("+150", result, "Positive delta string must contain '+N'.");
            StringAssert.Contains("\u2191", result, "Positive delta must include an up-arrow ↑.");
        }

        [Test]
        public void FormatTrend_NegativeDelta_ContainsMinusAndDownArrow()
        {
            MethodInfo m = GetMethod(typeof(ScoreHistoryController), "FormatTrend");
            string result = (string)m.Invoke(null, new object[] { -75 });
            StringAssert.Contains("-75", result, "Negative delta string must contain '-N'.");
            StringAssert.Contains("\u2193", result, "Negative delta must include a down-arrow ↓.");
        }
    }
}
