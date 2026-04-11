using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="DailyChallengeSO"/>.
    ///
    /// Covers:
    ///   • Fresh-instance defaults (CurrentChallenge null, IsCompleted false,
    ///     LastRefreshDate empty, CurrentIndex -1).
    ///   • <see cref="DailyChallengeSO.RefreshIfNeeded"/>:
    ///       – null config / empty pool → DoesNotThrow.
    ///       – new day (date pre-set to past) → sets CurrentChallenge, updates date.
    ///       – same day → preserves existing challenge.
    ///       – after LoadSnapshot same-day → restores challenge from pool index.
    ///       – new day with prior IsCompleted=true → resets completion flag.
    ///   • <see cref="DailyChallengeSO.MarkCompleted"/>:
    ///       – sets IsCompleted true; idempotent; null event no-throw; fires exactly once.
    ///   • <see cref="DailyChallengeSO.LoadSnapshot"/> /
    ///     <see cref="DailyChallengeSO.TakeSnapshot"/>: round-trip semantics.
    ///   • <see cref="DailyChallengeSO.Reset"/>: clears all fields silently.
    /// </summary>
    public class DailyChallengeSOTests
    {
        private DailyChallengeSO     _so;
        private DailyChallengeConfig _config;
        private BonusConditionSO     _conditionA;
        private VoidGameEvent        _onCompleted;

        // ── Reflection helpers ────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _so          = ScriptableObject.CreateInstance<DailyChallengeSO>();
            _config      = ScriptableObject.CreateInstance<DailyChallengeConfig>();
            _conditionA  = ScriptableObject.CreateInstance<BonusConditionSO>();
            _onCompleted = ScriptableObject.CreateInstance<VoidGameEvent>();

            // Wire one condition into the config pool.
            SetField(_config, "_challengePool",
                new List<BonusConditionSO> { _conditionA });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_so);
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_conditionA);
            Object.DestroyImmediate(_onCompleted);
        }

        // ── Fresh instance ────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_CurrentChallenge_IsNull()
        {
            Assert.IsNull(_so.CurrentChallenge);
        }

        [Test]
        public void FreshInstance_IsCompleted_IsFalse()
        {
            Assert.IsFalse(_so.IsCompleted);
        }

        [Test]
        public void FreshInstance_LastRefreshDate_IsEmpty()
        {
            Assert.AreEqual("", _so.LastRefreshDate);
        }

        // ── RefreshIfNeeded — null / empty guards ─────────────────────────────

        [Test]
        public void RefreshIfNeeded_NullConfig_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _so.RefreshIfNeeded(null));
        }

        [Test]
        public void RefreshIfNeeded_EmptyPool_DoesNotThrow()
        {
            var empty = ScriptableObject.CreateInstance<DailyChallengeConfig>();
            Assert.DoesNotThrow(() => _so.RefreshIfNeeded(empty));
            Object.DestroyImmediate(empty);
        }

        // ── RefreshIfNeeded — new-day selection ───────────────────────────────

        [Test]
        public void RefreshIfNeeded_NewDay_SetsCurrentChallenge()
        {
            // Pre-date to a past date so the SO treats this as a new day.
            SetField(_so, "_lastRefreshDate", "1900-01-01");
            _so.RefreshIfNeeded(_config);
            Assert.IsNotNull(_so.CurrentChallenge,
                "CurrentChallenge must be set after a new-day refresh.");
            Assert.AreEqual(_conditionA, _so.CurrentChallenge);
        }

        [Test]
        public void RefreshIfNeeded_NewDay_SetsLastRefreshDateToToday()
        {
            SetField(_so, "_lastRefreshDate", "1900-01-01");
            _so.RefreshIfNeeded(_config);
            Assert.AreEqual(DailyChallengeSO.TodayUtcString(), _so.LastRefreshDate);
        }

        [Test]
        public void RefreshIfNeeded_NewDay_ResetsCompletionFlag()
        {
            // Simulate a completed "old" challenge, then a new day begins.
            SetField(_so, "_lastRefreshDate", "1900-01-01");
            SetField(_so, "_isCompleted", true);
            _so.RefreshIfNeeded(_config);
            Assert.IsFalse(_so.IsCompleted,
                "IsCompleted must be reset to false when a new day's challenge is selected.");
        }

        // ── RefreshIfNeeded — same-day behaviour ──────────────────────────────

        [Test]
        public void RefreshIfNeeded_SameDay_PreservesExistingChallenge()
        {
            // Pre-set today's date and a challenge reference.
            SetField(_so, "_lastRefreshDate", DailyChallengeSO.TodayUtcString());
            SetField(_so, "_currentChallenge", _conditionA);
            _so.RefreshIfNeeded(_config);
            Assert.AreEqual(_conditionA, _so.CurrentChallenge,
                "Same-day refresh must not replace the existing challenge.");
        }

        [Test]
        public void RefreshIfNeeded_AfterLoadSnapshot_RestoresChallengeFromPoolIndex()
        {
            // GameBootstrapper calls LoadSnapshot; DailyChallengeManager.Awake then
            // calls RefreshIfNeeded to restore the challenge from the saved pool index.
            _so.LoadSnapshot(DailyChallengeSO.TodayUtcString(), 0, false);
            _so.RefreshIfNeeded(_config);
            // Pool[0] = _conditionA
            Assert.AreEqual(_conditionA, _so.CurrentChallenge,
                "Same-day restore must recover CurrentChallenge from the saved pool index.");
        }

        // ── MarkCompleted ─────────────────────────────────────────────────────

        [Test]
        public void MarkCompleted_SetsIsCompleted_True()
        {
            _so.MarkCompleted();
            Assert.IsTrue(_so.IsCompleted);
        }

        [Test]
        public void MarkCompleted_Idempotent_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                _so.MarkCompleted();
                _so.MarkCompleted();
            });
        }

        [Test]
        public void MarkCompleted_NullInspectorEvent_DoesNotThrow()
        {
            // _onChallengeCompleted not assigned — must be null-safe.
            Assert.DoesNotThrow(() => _so.MarkCompleted());
        }

        [Test]
        public void MarkCompleted_FiresEvent_ExactlyOnce()
        {
            SetField(_so, "_onChallengeCompleted", _onCompleted);
            int fireCount = 0;
            _onCompleted.RegisterCallback(() => fireCount++);

            _so.MarkCompleted();
            _so.MarkCompleted(); // second call must be a no-op

            Assert.AreEqual(1, fireCount,
                "MarkCompleted() must fire _onChallengeCompleted exactly once.");
        }

        // ── LoadSnapshot / TakeSnapshot round-trip ────────────────────────────

        [Test]
        public void LoadSnapshot_SetsPersistenceFields()
        {
            _so.LoadSnapshot("2026-04-15", 2, true);
            Assert.AreEqual("2026-04-15", _so.LastRefreshDate);
            Assert.AreEqual(2,    _so.CurrentIndex);
            Assert.IsTrue(_so.IsCompleted);
        }

        [Test]
        public void TakeSnapshot_ReturnsCurrentState()
        {
            _so.LoadSnapshot("2026-04-15", 1, false);
            var (date, index, completed) = _so.TakeSnapshot();
            Assert.AreEqual("2026-04-15", date);
            Assert.AreEqual(1,     index);
            Assert.IsFalse(completed);
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void Reset_ClearsAllFields()
        {
            SetField(_so, "_lastRefreshDate",  "2026-04-15");
            SetField(_so, "_currentChallenge", _conditionA);
            SetField(_so, "_currentIndex",     3);
            SetField(_so, "_isCompleted",      true);

            _so.Reset();

            Assert.IsNull(_so.CurrentChallenge);
            Assert.AreEqual("", _so.LastRefreshDate);
            Assert.AreEqual(-1, _so.CurrentIndex);
            Assert.IsFalse(_so.IsCompleted);
        }
    }
}
