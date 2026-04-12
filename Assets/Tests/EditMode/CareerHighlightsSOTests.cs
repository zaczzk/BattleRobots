using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="CareerHighlightsSO"/>.
    ///
    /// Covers:
    ///   • Fresh-instance defaults (all zero, all IsNew* false).
    ///   • <see cref="CareerHighlightsSO.Update(MatchResultSO)"/>:
    ///       - Null result is a safe no-op.
    ///       - BestSingleMatchDamage updates when higher; does not when lower.
    ///       - IsNewBestDamage flag set/cleared correctly.
    ///       - FastestWinSeconds set on first win.
    ///       - FastestWinSeconds updated when new match is shorter.
    ///       - FastestWinSeconds NOT updated on a loss.
    ///       - FastestWinSeconds NOT updated when new match is longer.
    ///       - BestSingleMatchCurrency updates when higher; does not when lower.
    ///       - LongestMatchSeconds updates when longer; does not when shorter.
    ///       - Event fires only when at least one record is broken.
    ///       - Event does NOT fire when no record is broken.
    ///   • <see cref="CareerHighlightsSO.LoadSnapshot"/>:
    ///       - Restores all fields; clears IsNew* flags; does not fire event.
    ///       - Null snapshot is a safe no-op.
    ///   • <see cref="CareerHighlightsSO.TakeSnapshot"/>:
    ///       - Returns current field values.
    ///   • <see cref="CareerHighlightsSO.Reset"/>:
    ///       - Zeroes all fields; clears flags; does not fire event.
    /// </summary>
    public class CareerHighlightsSOTests
    {
        private CareerHighlightsSO _so;
        private VoidGameEvent      _onUpdated;

        // ── Reflection helpers ────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private void WireEvent()
        {
            SetField(_so, "_onHighlightsUpdated", _onUpdated);
        }

        // ── MatchResultSO factory ─────────────────────────────────────────────

        private static MatchResultSO MakeResult(
            bool  playerWon      = false,
            float duration       = 60f,
            float damageDone     = 0f,
            int   currencyEarned = 0)
        {
            var r = ScriptableObject.CreateInstance<MatchResultSO>();
            r.Write(playerWon, duration, currencyEarned, 0, damageDone);
            return r;
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _so        = ScriptableObject.CreateInstance<CareerHighlightsSO>();
            _onUpdated = ScriptableObject.CreateInstance<VoidGameEvent>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_so);
            Object.DestroyImmediate(_onUpdated);
        }

        // ── Fresh instance ────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_AllFields_AreZero()
        {
            Assert.AreEqual(0f, _so.BestSingleMatchDamage);
            Assert.AreEqual(0f, _so.FastestWinSeconds);
            Assert.AreEqual(0,  _so.BestSingleMatchCurrency);
            Assert.AreEqual(0f, _so.LongestMatchSeconds);
        }

        [Test]
        public void FreshInstance_AllNewFlags_AreFalse()
        {
            Assert.IsFalse(_so.IsNewBestDamage);
            Assert.IsFalse(_so.IsNewFastestWin);
            Assert.IsFalse(_so.IsNewBestCurrency);
            Assert.IsFalse(_so.IsNewLongestMatch);
        }

        // ── Update — null result ──────────────────────────────────────────────

        [Test]
        public void Update_NullResult_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _so.Update(null),
                "Update(null) must not throw.");
        }

        [Test]
        public void Update_NullResult_LeavesFieldsUnchanged()
        {
            _so.Update(null);
            Assert.AreEqual(0f, _so.BestSingleMatchDamage,
                "Update(null) must not modify BestSingleMatchDamage.");
            Assert.IsFalse(_so.IsNewBestDamage,
                "Update(null) must not set IsNewBestDamage.");
        }

        // ── Update — BestSingleMatchDamage ────────────────────────────────────

        [Test]
        public void Update_SetsBestDamage_WhenHigher()
        {
            var result = MakeResult(damageDone: 300f);
            _so.Update(result);
            Assert.AreEqual(300f, _so.BestSingleMatchDamage);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Update_SetsIsNewBestDamage_WhenHigher()
        {
            var result = MakeResult(damageDone: 300f);
            _so.Update(result);
            Assert.IsTrue(_so.IsNewBestDamage);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Update_DoesNotSetBestDamage_WhenLower()
        {
            var r1 = MakeResult(damageDone: 500f);
            var r2 = MakeResult(damageDone: 200f);
            _so.Update(r1);
            _so.Update(r2);
            Assert.AreEqual(500f, _so.BestSingleMatchDamage);
            Assert.IsFalse(_so.IsNewBestDamage);
            Object.DestroyImmediate(r1);
            Object.DestroyImmediate(r2);
        }

        // ── Update — FastestWinSeconds ────────────────────────────────────────

        [Test]
        public void Update_SetsFastestWin_OnFirstWin()
        {
            var result = MakeResult(playerWon: true, duration: 90f);
            _so.Update(result);
            Assert.AreEqual(90f, _so.FastestWinSeconds);
            Assert.IsTrue(_so.IsNewFastestWin);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Update_SetsFastestWin_WhenShorterThanPrevious()
        {
            var r1 = MakeResult(playerWon: true, duration: 90f);
            var r2 = MakeResult(playerWon: true, duration: 45f);
            _so.Update(r1);
            _so.Update(r2);
            Assert.AreEqual(45f, _so.FastestWinSeconds);
            Assert.IsTrue(_so.IsNewFastestWin);
            Object.DestroyImmediate(r1);
            Object.DestroyImmediate(r2);
        }

        [Test]
        public void Update_DoesNotSetFastestWin_OnLoss()
        {
            var result = MakeResult(playerWon: false, duration: 30f);
            _so.Update(result);
            Assert.AreEqual(0f, _so.FastestWinSeconds,
                "A loss must not update FastestWinSeconds.");
            Assert.IsFalse(_so.IsNewFastestWin);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Update_DoesNotSetFastestWin_WhenLongerThanPrevious()
        {
            var r1 = MakeResult(playerWon: true, duration: 50f);
            var r2 = MakeResult(playerWon: true, duration: 120f);
            _so.Update(r1);
            _so.Update(r2);
            Assert.AreEqual(50f, _so.FastestWinSeconds,
                "A longer win must not overwrite a shorter FastestWinSeconds.");
            Assert.IsFalse(_so.IsNewFastestWin);
            Object.DestroyImmediate(r1);
            Object.DestroyImmediate(r2);
        }

        // ── Update — BestSingleMatchCurrency ──────────────────────────────────

        [Test]
        public void Update_SetsBestCurrency_WhenHigher()
        {
            var result = MakeResult(currencyEarned: 400);
            _so.Update(result);
            Assert.AreEqual(400, _so.BestSingleMatchCurrency);
            Assert.IsTrue(_so.IsNewBestCurrency);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Update_DoesNotSetBestCurrency_WhenLower()
        {
            var r1 = MakeResult(currencyEarned: 400);
            var r2 = MakeResult(currencyEarned: 100);
            _so.Update(r1);
            _so.Update(r2);
            Assert.AreEqual(400, _so.BestSingleMatchCurrency);
            Assert.IsFalse(_so.IsNewBestCurrency);
            Object.DestroyImmediate(r1);
            Object.DestroyImmediate(r2);
        }

        // ── Update — LongestMatchSeconds ──────────────────────────────────────

        [Test]
        public void Update_SetsLongestMatch_WhenLonger()
        {
            var result = MakeResult(duration: 120f);
            _so.Update(result);
            Assert.AreEqual(120f, _so.LongestMatchSeconds);
            Assert.IsTrue(_so.IsNewLongestMatch);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Update_DoesNotSetLongestMatch_WhenShorter()
        {
            var r1 = MakeResult(duration: 120f);
            var r2 = MakeResult(duration: 30f);
            _so.Update(r1);
            _so.Update(r2);
            Assert.AreEqual(120f, _so.LongestMatchSeconds);
            Assert.IsFalse(_so.IsNewLongestMatch);
            Object.DestroyImmediate(r1);
            Object.DestroyImmediate(r2);
        }

        // ── Update — event firing ─────────────────────────────────────────────

        [Test]
        public void Update_FiresEvent_WhenRecordBroken()
        {
            WireEvent();
            int fireCount = 0;
            _onUpdated.RegisterCallback(() => fireCount++);

            var result = MakeResult(damageDone: 100f);
            _so.Update(result);

            Assert.AreEqual(1, fireCount,
                "_onHighlightsUpdated must fire when a new record is set.");
            Object.DestroyImmediate(result);
        }

        [Test]
        public void Update_DoesNotFireEvent_WhenNoRecordBroken()
        {
            WireEvent();
            // Prime all categories with high values so the next match breaks nothing.
            var r1 = MakeResult(playerWon: true, duration: 10f, damageDone: 999f, currencyEarned: 9999);
            _so.Update(r1);

            int fireCount = 0;
            _onUpdated.RegisterCallback(() => fireCount++);

            // New match with lower values; only LongestMatch could update, but duration < 10 min.
            // Use duration=5f (shorter than 10f) so no record is broken.
            var r2 = MakeResult(playerWon: false, duration: 5f, damageDone: 1f, currencyEarned: 1);
            _so.Update(r2);

            Assert.AreEqual(0, fireCount,
                "_onHighlightsUpdated must NOT fire when no record is broken.");
            Object.DestroyImmediate(r1);
            Object.DestroyImmediate(r2);
        }

        // ── LoadSnapshot ──────────────────────────────────────────────────────

        [Test]
        public void LoadSnapshot_RestoresAllFields()
        {
            var snap = new CareerHighlightsSnapshot
            {
                bestSingleMatchDamage   = 250f,
                fastestWinSeconds       = 40f,
                bestSingleMatchCurrency = 300,
                longestMatchSeconds     = 180f,
            };
            _so.LoadSnapshot(snap);

            Assert.AreEqual(250f, _so.BestSingleMatchDamage);
            Assert.AreEqual(40f,  _so.FastestWinSeconds);
            Assert.AreEqual(300,  _so.BestSingleMatchCurrency);
            Assert.AreEqual(180f, _so.LongestMatchSeconds);
        }

        [Test]
        public void LoadSnapshot_ClearsNewFlags()
        {
            // Update first to set flags true.
            var r = MakeResult(playerWon: true, duration: 30f, damageDone: 100f, currencyEarned: 200);
            _so.Update(r);
            Object.DestroyImmediate(r);

            // Load clears them all.
            _so.LoadSnapshot(new CareerHighlightsSnapshot());

            Assert.IsFalse(_so.IsNewBestDamage);
            Assert.IsFalse(_so.IsNewFastestWin);
            Assert.IsFalse(_so.IsNewBestCurrency);
            Assert.IsFalse(_so.IsNewLongestMatch);
        }

        [Test]
        public void LoadSnapshot_NullSnapshot_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _so.LoadSnapshot(null),
                "LoadSnapshot(null) must not throw.");
        }

        [Test]
        public void LoadSnapshot_DoesNotFireEvent()
        {
            WireEvent();
            int fireCount = 0;
            _onUpdated.RegisterCallback(() => fireCount++);

            _so.LoadSnapshot(new CareerHighlightsSnapshot
            {
                bestSingleMatchDamage = 500f,
                fastestWinSeconds     = 20f,
            });

            Assert.AreEqual(0, fireCount,
                "LoadSnapshot must not fire _onHighlightsUpdated.");
        }

        // ── TakeSnapshot ──────────────────────────────────────────────────────

        [Test]
        public void TakeSnapshot_ReturnsCurrentValues()
        {
            var r = MakeResult(playerWon: true, duration: 55f, damageDone: 200f, currencyEarned: 150);
            _so.Update(r);
            Object.DestroyImmediate(r);

            CareerHighlightsSnapshot snap = _so.TakeSnapshot();

            Assert.AreEqual(200f, snap.bestSingleMatchDamage);
            Assert.AreEqual(55f,  snap.fastestWinSeconds);
            Assert.AreEqual(150,  snap.bestSingleMatchCurrency);
            Assert.AreEqual(55f,  snap.longestMatchSeconds);
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void Reset_ClearsAllFieldsAndFlags()
        {
            var r = MakeResult(playerWon: true, duration: 60f, damageDone: 400f, currencyEarned: 500);
            _so.Update(r);
            Object.DestroyImmediate(r);

            _so.Reset();

            Assert.AreEqual(0f, _so.BestSingleMatchDamage);
            Assert.AreEqual(0f, _so.FastestWinSeconds);
            Assert.AreEqual(0,  _so.BestSingleMatchCurrency);
            Assert.AreEqual(0f, _so.LongestMatchSeconds);
            Assert.IsFalse(_so.IsNewBestDamage);
            Assert.IsFalse(_so.IsNewFastestWin);
            Assert.IsFalse(_so.IsNewBestCurrency);
            Assert.IsFalse(_so.IsNewLongestMatch);
        }

        [Test]
        public void Reset_DoesNotFireEvent()
        {
            WireEvent();
            var r = MakeResult(damageDone: 100f);
            _so.Update(r);
            Object.DestroyImmediate(r);

            int fireCount = 0;
            _onUpdated.RegisterCallback(() => fireCount++);

            _so.Reset();

            Assert.AreEqual(0, fireCount,
                "Reset() must not fire _onHighlightsUpdated.");
        }
    }
}
