using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PersonalBestSO"/>.
    ///
    /// Covers:
    ///   • Fresh-instance defaults (CurrentScore=0, BestScore=0, IsNewBest=false).
    ///   • <see cref="PersonalBestSO.Submit(int)"/>:
    ///       - First submission is always a new best.
    ///       - Higher score updates BestScore and sets IsNewBest=true.
    ///       - Lower score does NOT update BestScore; IsNewBest=false.
    ///       - Same score as current best: NOT a new best (strict greater-than).
    ///       - Negative values clamped to 0.
    ///       - Fires _onScoreSubmitted every call.
    ///       - Fires _onNewPersonalBest only when a new best is set.
    ///       - Null channels do not throw.
    ///       - BestScore never decreases after a higher score has been set.
    ///       - Return value matches IsNewBest.
    ///   • <see cref="PersonalBestSO.LoadSnapshot(int)"/>:
    ///       - Restores BestScore; resets CurrentScore and IsNewBest.
    ///       - Negative values clamped to 0.
    ///       - Does NOT fire any event.
    ///   • <see cref="PersonalBestSO.Reset"/>:
    ///       - Zeroes all fields.
    ///       - Does NOT fire any event.
    /// </summary>
    public class PersonalBestSOTests
    {
        private PersonalBestSO _so;
        private VoidGameEvent  _onNewBest;
        private VoidGameEvent  _onSubmitted;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private void WireEvents()
        {
            SetField(_so, "_onNewPersonalBest", _onNewBest);
            SetField(_so, "_onScoreSubmitted",  _onSubmitted);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _so          = ScriptableObject.CreateInstance<PersonalBestSO>();
            _onNewBest   = ScriptableObject.CreateInstance<VoidGameEvent>();
            _onSubmitted = ScriptableObject.CreateInstance<VoidGameEvent>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_so);
            Object.DestroyImmediate(_onNewBest);
            Object.DestroyImmediate(_onSubmitted);
        }

        // ── Fresh instance ────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_CurrentScore_IsZero()
        {
            Assert.AreEqual(0, _so.CurrentScore);
        }

        [Test]
        public void FreshInstance_BestScore_IsZero()
        {
            Assert.AreEqual(0, _so.BestScore);
        }

        [Test]
        public void FreshInstance_IsNewBest_IsFalse()
        {
            Assert.IsFalse(_so.IsNewBest);
        }

        // ── Submit — basic contracts ──────────────────────────────────────────

        [Test]
        public void Submit_UpdatesCurrentScore()
        {
            _so.Submit(500);
            Assert.AreEqual(500, _so.CurrentScore);
        }

        [Test]
        public void Submit_FirstPositiveScore_IsNewBest_True()
        {
            bool result = _so.Submit(100);
            Assert.IsTrue(result);
            Assert.IsTrue(_so.IsNewBest);
        }

        [Test]
        public void Submit_HigherScore_UpdatesBestScore()
        {
            _so.Submit(300);
            _so.Submit(500);
            Assert.AreEqual(500, _so.BestScore);
        }

        [Test]
        public void Submit_HigherScore_IsNewBest_True()
        {
            _so.Submit(300);
            bool result = _so.Submit(500);
            Assert.IsTrue(result);
            Assert.IsTrue(_so.IsNewBest);
        }

        [Test]
        public void Submit_LowerScore_DoesNotUpdateBestScore()
        {
            _so.Submit(500);
            _so.Submit(200);
            Assert.AreEqual(500, _so.BestScore);
        }

        [Test]
        public void Submit_LowerScore_IsNewBest_False()
        {
            _so.Submit(500);
            bool result = _so.Submit(200);
            Assert.IsFalse(result);
            Assert.IsFalse(_so.IsNewBest);
        }

        [Test]
        public void Submit_EqualScore_IsNewBest_False()
        {
            // Strict greater-than: same score is NOT a new personal best.
            _so.Submit(400);
            bool result = _so.Submit(400);
            Assert.IsFalse(result);
            Assert.IsFalse(_so.IsNewBest);
        }

        [Test]
        public void Submit_NegativeScore_ClampedToZero()
        {
            _so.Submit(-50);
            Assert.AreEqual(0, _so.CurrentScore);
        }

        [Test]
        public void Submit_BestScore_NeverDecreases()
        {
            _so.Submit(800);
            _so.Submit(100);
            Assert.AreEqual(800, _so.BestScore,
                "BestScore must never decrease after a higher score has been set.");
        }

        // ── Submit — event channels ───────────────────────────────────────────

        [Test]
        public void Submit_AlwaysFires_OnScoreSubmitted()
        {
            WireEvents();
            int submittedCount = 0;
            _onSubmitted.RegisterCallback(() => submittedCount++);

            _so.Submit(100);
            _so.Submit(50);

            Assert.AreEqual(2, submittedCount,
                "_onScoreSubmitted must fire on every Submit() call.");
        }

        [Test]
        public void Submit_NewBest_Fires_OnNewPersonalBest()
        {
            WireEvents();
            int newBestCount = 0;
            _onNewBest.RegisterCallback(() => newBestCount++);

            _so.Submit(200); // first score — new best
            _so.Submit(500); // higher — new best again

            Assert.AreEqual(2, newBestCount,
                "_onNewPersonalBest must fire each time a new best is set.");
        }

        [Test]
        public void Submit_NotNewBest_DoesNotFire_OnNewPersonalBest()
        {
            WireEvents();
            int newBestCount = 0;
            _onNewBest.RegisterCallback(() => newBestCount++);

            _so.Submit(500); // new best (count = 1)
            _so.Submit(300); // not a new best — must NOT fire

            Assert.AreEqual(1, newBestCount,
                "_onNewPersonalBest must not fire when score does not exceed BestScore.");
        }

        [Test]
        public void Submit_NullChannels_DoesNotThrow()
        {
            // Both channels left null (not wired).
            Assert.DoesNotThrow(() => _so.Submit(100),
                "Submit() with null event channels must not throw.");
        }

        // ── LoadSnapshot ──────────────────────────────────────────────────────

        [Test]
        public void LoadSnapshot_SetsBestScore()
        {
            _so.LoadSnapshot(750);
            Assert.AreEqual(750, _so.BestScore);
        }

        [Test]
        public void LoadSnapshot_ResetsCurrentScore_ToZero()
        {
            _so.Submit(300);              // set a current score first
            _so.LoadSnapshot(750);
            Assert.AreEqual(0, _so.CurrentScore,
                "LoadSnapshot must reset CurrentScore to 0.");
        }

        [Test]
        public void LoadSnapshot_ResetsIsNewBest_ToFalse()
        {
            _so.Submit(300);              // IsNewBest = true
            _so.LoadSnapshot(750);
            Assert.IsFalse(_so.IsNewBest,
                "LoadSnapshot must reset IsNewBest to false.");
        }

        [Test]
        public void LoadSnapshot_NegativeValue_ClampedToZero()
        {
            _so.LoadSnapshot(-100);
            Assert.AreEqual(0, _so.BestScore);
        }

        [Test]
        public void LoadSnapshot_DoesNotFireAnyEvent()
        {
            WireEvents();
            int fireCount = 0;
            _onNewBest.RegisterCallback(()   => fireCount++);
            _onSubmitted.RegisterCallback(() => fireCount++);

            _so.LoadSnapshot(500);

            Assert.AreEqual(0, fireCount,
                "LoadSnapshot() must not fire any event channel.");
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void Reset_ZeroesAllFields()
        {
            _so.Submit(800);       // set a non-zero best
            _so.Reset();
            Assert.AreEqual(0, _so.CurrentScore, "CurrentScore must be 0 after Reset.");
            Assert.AreEqual(0, _so.BestScore,    "BestScore must be 0 after Reset.");
            Assert.IsFalse(_so.IsNewBest,         "IsNewBest must be false after Reset.");
        }

        [Test]
        public void Reset_DoesNotFireAnyEvent()
        {
            WireEvents();
            int fireCount = 0;
            _onNewBest.RegisterCallback(()   => fireCount++);
            _onSubmitted.RegisterCallback(() => fireCount++);

            _so.Submit(400); // fire count = 2 (new-best + submitted)
            _so.Reset();     // must NOT fire

            Assert.AreEqual(2, fireCount,
                "Reset() must not fire any event channel (count must stay at 2).");
        }
    }
}
