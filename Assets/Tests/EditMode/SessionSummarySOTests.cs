using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;
using BattleRobots.UI;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="SessionSummarySO"/> and
    /// <see cref="SessionSummaryController"/>.
    ///
    /// SessionSummarySOTests covers:
    ///   • Fresh-instance defaults (all zero, WinRate 0, WinRatePercent "0%").
    ///   • RecordMatch(null) → safe no-op (no increment, no event).
    ///   • RecordMatch(win) → increments MatchesPlayed, Wins, TotalCurrencyEarned, fires event.
    ///   • RecordMatch(loss) → increments MatchesPlayed and TotalCurrencyEarned; Wins unchanged.
    ///   • WinRate: one win of one match → 1.0f; one win of two → 0.5f.
    ///   • WinRatePercent contains '%' character.
    ///   • WinRatePercent after 1 win / 1 match → "100%".
    ///   • WinRatePercent after 1 win / 2 matches → "50%".
    ///   • Reset clears MatchesPlayed, Wins, TotalCurrencyEarned.
    ///   • Reset is silent (does not fire _onSessionUpdated).
    ///
    /// SessionSummaryControllerTests covers:
    ///   • OnEnable / OnDisable with all-null inspector refs → no throw.
    ///   • OnEnable / OnDisable with null event channel → no throw.
    ///   • OnDisable unregisters refresh delegate from _onSessionUpdated.
    ///   • Refresh() with null _sessionSummary → labels show em-dash ('—').
    ///   • Refresh() with data → _matchesPlayedText shows MatchesPlayed.
    ///   • Refresh() with data → _winsText shows Wins.
    ///
    /// All tests run headless (no Unity Editor scene required).
    /// Reflection is used to inject private serialized fields.
    /// </summary>
    public class SessionSummarySOTests
    {
        // ── Instances under test ──────────────────────────────────────────────

        private SessionSummarySO _so;
        private VoidGameEvent    _onUpdated;

        // ── Reflection helpers ────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── MatchResultSO factory ─────────────────────────────────────────────

        private static MatchResultSO MakeResult(bool playerWon, int currencyEarned)
        {
            var r = ScriptableObject.CreateInstance<MatchResultSO>();
            r.Write(playerWon, 60f, currencyEarned, currencyEarned);
            return r;
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _so        = ScriptableObject.CreateInstance<SessionSummarySO>();
            _onUpdated = ScriptableObject.CreateInstance<VoidGameEvent>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_so);
            Object.DestroyImmediate(_onUpdated);
        }

        // ── Fresh-instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_MatchesPlayedIsZero()
        {
            Assert.AreEqual(0, _so.MatchesPlayed, "Fresh instance must have MatchesPlayed == 0.");
        }

        [Test]
        public void FreshInstance_WinsIsZero()
        {
            Assert.AreEqual(0, _so.Wins, "Fresh instance must have Wins == 0.");
        }

        [Test]
        public void FreshInstance_TotalCurrencyEarnedIsZero()
        {
            Assert.AreEqual(0, _so.TotalCurrencyEarned,
                "Fresh instance must have TotalCurrencyEarned == 0.");
        }

        [Test]
        public void FreshInstance_WinRateIsZero_SafeDivision()
        {
            Assert.AreEqual(0f, _so.WinRate,
                "WinRate must be 0 when no matches have been played (safe division).");
        }

        [Test]
        public void FreshInstance_WinRatePercentIs0Percent()
        {
            Assert.AreEqual("0%", _so.WinRatePercent,
                "WinRatePercent must be '0%' when no matches have been played.");
        }

        // ── RecordMatch — null guard ───────────────────────────────────────────

        [Test]
        public void RecordMatch_NullResult_IsNoOp()
        {
            SetField(_so, "_onSessionUpdated", _onUpdated);
            int eventCount = 0;
            _onUpdated.RegisterCallback(() => eventCount++);

            _so.RecordMatch(null);

            Assert.AreEqual(0, _so.MatchesPlayed, "Null result must not increment MatchesPlayed.");
            Assert.AreEqual(0, eventCount, "Null result must not fire _onSessionUpdated.");
        }

        // ── RecordMatch — win ─────────────────────────────────────────────────

        [Test]
        public void RecordMatch_Win_IncrementsMatchesPlayed()
        {
            var result = MakeResult(true, 200);
            _so.RecordMatch(result);
            Assert.AreEqual(1, _so.MatchesPlayed, "RecordMatch(win) must increment MatchesPlayed.");
            Object.DestroyImmediate(result);
        }

        [Test]
        public void RecordMatch_Win_IncrementsWins()
        {
            var result = MakeResult(true, 200);
            _so.RecordMatch(result);
            Assert.AreEqual(1, _so.Wins, "RecordMatch(win) must increment Wins.");
            Object.DestroyImmediate(result);
        }

        [Test]
        public void RecordMatch_Win_IncrementsTotalCurrencyEarned()
        {
            var result = MakeResult(true, 350);
            _so.RecordMatch(result);
            Assert.AreEqual(350, _so.TotalCurrencyEarned,
                "RecordMatch(win) must add CurrencyEarned to TotalCurrencyEarned.");
            Object.DestroyImmediate(result);
        }

        [Test]
        public void RecordMatch_FiresOnSessionUpdated()
        {
            SetField(_so, "_onSessionUpdated", _onUpdated);
            int eventCount = 0;
            _onUpdated.RegisterCallback(() => eventCount++);

            var result = MakeResult(true, 200);
            _so.RecordMatch(result);

            Assert.AreEqual(1, eventCount, "RecordMatch must fire _onSessionUpdated.");
            Object.DestroyImmediate(result);
        }

        // ── RecordMatch — loss ────────────────────────────────────────────────

        [Test]
        public void RecordMatch_Loss_IncrementsMatchesPlayedNotWins()
        {
            var result = MakeResult(false, 50);
            _so.RecordMatch(result);
            Assert.AreEqual(1, _so.MatchesPlayed, "RecordMatch(loss) must increment MatchesPlayed.");
            Assert.AreEqual(0, _so.Wins,          "RecordMatch(loss) must not increment Wins.");
            Object.DestroyImmediate(result);
        }

        // ── WinRate / WinRatePercent ───────────────────────────────────────────

        [Test]
        public void WinRate_OneWinOneMatch_IsOne()
        {
            var result = MakeResult(true, 200);
            _so.RecordMatch(result);
            Assert.AreEqual(1f, _so.WinRate, 0.001f,
                "One win out of one match must produce WinRate == 1.0.");
            Object.DestroyImmediate(result);
        }

        [Test]
        public void WinRate_OneWinTwoMatches_IsHalf()
        {
            var win  = MakeResult(true,  200);
            var loss = MakeResult(false, 50);
            _so.RecordMatch(win);
            _so.RecordMatch(loss);
            Assert.AreEqual(0.5f, _so.WinRate, 0.001f,
                "One win out of two matches must produce WinRate == 0.5.");
            Object.DestroyImmediate(win);
            Object.DestroyImmediate(loss);
        }

        [Test]
        public void WinRatePercent_ContainsPercentSign()
        {
            var result = MakeResult(true, 100);
            _so.RecordMatch(result);
            StringAssert.Contains("%", _so.WinRatePercent,
                "WinRatePercent must contain the '%' character.");
            Object.DestroyImmediate(result);
        }

        [Test]
        public void WinRatePercent_AfterOneWinOneMatch_Is100Percent()
        {
            var result = MakeResult(true, 200);
            _so.RecordMatch(result);
            Assert.AreEqual("100%", _so.WinRatePercent,
                "One win out of one match must produce WinRatePercent == '100%'.");
            Object.DestroyImmediate(result);
        }

        [Test]
        public void WinRatePercent_AfterOneWinTwoMatches_Is50Percent()
        {
            var win  = MakeResult(true,  200);
            var loss = MakeResult(false, 50);
            _so.RecordMatch(win);
            _so.RecordMatch(loss);
            Assert.AreEqual("50%", _so.WinRatePercent,
                "One win out of two matches must produce WinRatePercent == '50%'.");
            Object.DestroyImmediate(win);
            Object.DestroyImmediate(loss);
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void Reset_ClearsMatchesPlayedWinsAndCurrency()
        {
            var win  = MakeResult(true,  200);
            var loss = MakeResult(false, 50);
            _so.RecordMatch(win);
            _so.RecordMatch(loss);

            _so.Reset();

            Assert.AreEqual(0, _so.MatchesPlayed,       "Reset must clear MatchesPlayed.");
            Assert.AreEqual(0, _so.Wins,                "Reset must clear Wins.");
            Assert.AreEqual(0, _so.TotalCurrencyEarned, "Reset must clear TotalCurrencyEarned.");

            Object.DestroyImmediate(win);
            Object.DestroyImmediate(loss);
        }

        [Test]
        public void Reset_Silent_DoesNotFireOnSessionUpdated()
        {
            SetField(_so, "_onSessionUpdated", _onUpdated);
            int eventCount = 0;
            _onUpdated.RegisterCallback(() => eventCount++);

            var result = MakeResult(true, 200);
            _so.RecordMatch(result); // fires once
            eventCount = 0;          // reset counter after the record

            _so.Reset();

            Assert.AreEqual(0, eventCount,
                "Reset must be silent — must not fire _onSessionUpdated.");
            Object.DestroyImmediate(result);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SessionSummaryController tests
    // ═══════════════════════════════════════════════════════════════════════════

    public class SessionSummaryControllerTests
    {
        // ── Reflection helpers ────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static SessionSummaryController MakeController(out GameObject go)
        {
            go = new GameObject("SessionSummaryControllerTest");
            go.SetActive(false);
            return go.AddComponent<SessionSummaryController>();
        }

        private static MatchResultSO MakeResult(bool playerWon, int currencyEarned)
        {
            var r = ScriptableObject.CreateInstance<MatchResultSO>();
            r.Write(playerWon, 60f, currencyEarned, currencyEarned);
            return r;
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
            var summary = ScriptableObject.CreateInstance<SessionSummarySO>();
            SetField(go.GetComponent<SessionSummaryController>(), "_sessionSummary", summary);
            // _onSessionUpdated remains null
            Assert.DoesNotThrow(() => go.SetActive(true));
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(summary);
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
        public void OnDisable_UnregistersFromOnSessionUpdated()
        {
            var channel = ScriptableObject.CreateInstance<VoidGameEvent>();
            int externalCount = 0;
            channel.RegisterCallback(() => externalCount++);

            MakeController(out GameObject go);
            SetField(go.GetComponent<SessionSummaryController>(), "_onSessionUpdated", channel);

            go.SetActive(true);   // Awake + OnEnable → controller subscribed
            go.SetActive(false);  // OnDisable → controller must unsubscribe

            channel.Raise();

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(channel);

            Assert.AreEqual(1, externalCount,
                "Only the external counter should fire; controller must be unsubscribed.");
        }

        // ── Refresh — null summary ────────────────────────────────────────────

        [Test]
        public void Refresh_NullSummary_ShowsEmDashOnAllLabels()
        {
            MakeController(out GameObject go);
            var ctrl = go.GetComponent<SessionSummaryController>();

            // Wire up text components so we can inspect their values.
            var matchesGo  = new GameObject(); var matchesText  = matchesGo.AddComponent<UnityEngine.UI.Text>();
            var winsGo     = new GameObject(); var winsText     = winsGo.AddComponent<UnityEngine.UI.Text>();
            var winRateGo  = new GameObject(); var winRateText  = winRateGo.AddComponent<UnityEngine.UI.Text>();
            var currencyGo = new GameObject(); var currencyText = currencyGo.AddComponent<UnityEngine.UI.Text>();

            SetField(ctrl, "_matchesPlayedText",  matchesText);
            SetField(ctrl, "_winsText",           winsText);
            SetField(ctrl, "_winRateText",        winRateText);
            SetField(ctrl, "_currencyEarnedText", currencyText);
            // _sessionSummary remains null

            go.SetActive(true); // triggers Awake + OnEnable → Refresh()

            Assert.AreEqual("\u2014", matchesText.text,  "Null summary: _matchesPlayedText must show em-dash.");
            Assert.AreEqual("\u2014", winsText.text,     "Null summary: _winsText must show em-dash.");
            Assert.AreEqual("\u2014", winRateText.text,  "Null summary: _winRateText must show em-dash.");
            Assert.AreEqual("\u2014", currencyText.text, "Null summary: _currencyEarnedText must show em-dash.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(matchesGo);
            Object.DestroyImmediate(winsGo);
            Object.DestroyImmediate(winRateGo);
            Object.DestroyImmediate(currencyGo);
        }

        // ── Refresh — with data ───────────────────────────────────────────────

        [Test]
        public void Refresh_WithSummary_MatchesPlayedTextShowsCount()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<SessionSummaryController>();
            var summary = ScriptableObject.CreateInstance<SessionSummarySO>();

            var result = MakeResult(true, 200);
            summary.RecordMatch(result);  // MatchesPlayed = 1

            var labelGo = new GameObject();
            var label   = labelGo.AddComponent<UnityEngine.UI.Text>();
            SetField(ctrl, "_sessionSummary",    summary);
            SetField(ctrl, "_matchesPlayedText", label);

            go.SetActive(true);

            Assert.AreEqual("1", label.text,
                "Refresh must display MatchesPlayed as a string on _matchesPlayedText.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(labelGo);
            Object.DestroyImmediate(summary);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Refresh_WithSummary_WinsTextShowsWins()
        {
            MakeController(out GameObject go);
            var ctrl    = go.GetComponent<SessionSummaryController>();
            var summary = ScriptableObject.CreateInstance<SessionSummarySO>();

            var win  = MakeResult(true,  200);
            var loss = MakeResult(false, 50);
            summary.RecordMatch(win);
            summary.RecordMatch(loss);  // Wins = 1

            var labelGo = new GameObject();
            var label   = labelGo.AddComponent<UnityEngine.UI.Text>();
            SetField(ctrl, "_sessionSummary", summary);
            SetField(ctrl, "_winsText",       label);

            go.SetActive(true);

            Assert.AreEqual("1", label.text,
                "Refresh must display Wins as a string on _winsText.");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(labelGo);
            Object.DestroyImmediate(summary);
            Object.DestroyImmediate(win);
            Object.DestroyImmediate(loss);
        }
    }
}
