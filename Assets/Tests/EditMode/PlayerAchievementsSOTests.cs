using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="PlayerAchievementsSO"/>.
    ///
    /// Covers:
    ///   • Fresh-instance defaults (counters zero, lists empty, LastUnlockedId null).
    ///   • <see cref="PlayerAchievementsSO.HasUnlocked"/>: null/whitespace guards,
    ///     unknown ID, known ID.
    ///   • <see cref="PlayerAchievementsSO.Unlock"/>: first call sets state and fires
    ///     event; idempotent (second call no-op and no re-fire); null/whitespace guard.
    ///   • <see cref="PlayerAchievementsSO.RecordMatchResult"/>: win increments both
    ///     counters; loss only increments total; multiple sequential calls accumulate.
    ///   • <see cref="PlayerAchievementsSO.LoadSnapshot"/>: counter clamping, list
    ///     restoration, null-list safety, duplicate deduplication.
    ///   • <see cref="PlayerAchievementsSO.Reset"/>: silently clears all state
    ///     without firing the event.
    /// </summary>
    public class PlayerAchievementsSOTests
    {
        private PlayerAchievementsSO _so;
        private VoidGameEvent        _onUnlocked;

        // ── Reflection helper ─────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private void WireEvent()
        {
            SetField(_so, "_onAchievementUnlocked", _onUnlocked);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _so         = ScriptableObject.CreateInstance<PlayerAchievementsSO>();
            _onUnlocked = ScriptableObject.CreateInstance<VoidGameEvent>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_so);
            Object.DestroyImmediate(_onUnlocked);
        }

        // ── Fresh-instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_TotalMatchesPlayed_IsZero()
        {
            Assert.AreEqual(0, _so.TotalMatchesPlayed);
        }

        [Test]
        public void FreshInstance_TotalMatchesWon_IsZero()
        {
            Assert.AreEqual(0, _so.TotalMatchesWon);
        }

        [Test]
        public void FreshInstance_UnlockedIds_IsEmpty()
        {
            Assert.IsNotNull(_so.UnlockedIds);
            Assert.AreEqual(0, _so.UnlockedIds.Count);
        }

        [Test]
        public void FreshInstance_LastUnlockedId_IsNull()
        {
            Assert.IsNull(_so.LastUnlockedId);
        }

        // ── HasUnlocked ───────────────────────────────────────────────────────

        [Test]
        public void HasUnlocked_NullId_ReturnsFalse()
        {
            Assert.IsFalse(_so.HasUnlocked(null));
        }

        [Test]
        public void HasUnlocked_WhitespaceId_ReturnsFalse()
        {
            Assert.IsFalse(_so.HasUnlocked("   "));
        }

        [Test]
        public void HasUnlocked_UnknownId_ReturnsFalse()
        {
            Assert.IsFalse(_so.HasUnlocked("unknown_achievement"));
        }

        [Test]
        public void HasUnlocked_AfterUnlock_ReturnsTrue()
        {
            _so.Unlock("ach_001");
            Assert.IsTrue(_so.HasUnlocked("ach_001"));
        }

        // ── Unlock ────────────────────────────────────────────────────────────

        [Test]
        public void Unlock_NullId_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _so.Unlock(null));
        }

        [Test]
        public void Unlock_WhitespaceId_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _so.Unlock("  "));
        }

        [Test]
        public void Unlock_AddsToUnlockedIds()
        {
            _so.Unlock("ach_001");
            Assert.AreEqual(1, _so.UnlockedIds.Count);
            Assert.AreEqual("ach_001", _so.UnlockedIds[0]);
        }

        [Test]
        public void Unlock_SetsLastUnlockedId()
        {
            _so.Unlock("ach_002");
            Assert.AreEqual("ach_002", _so.LastUnlockedId);
        }

        [Test]
        public void Unlock_FiresEvent()
        {
            WireEvent();
            int count = 0;
            _onUnlocked.RegisterCallback(() => count++);

            _so.Unlock("ach_001");

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Unlock_Idempotent_DoesNotAddDuplicate()
        {
            _so.Unlock("ach_001");
            _so.Unlock("ach_001");
            Assert.AreEqual(1, _so.UnlockedIds.Count);
        }

        [Test]
        public void Unlock_Idempotent_DoesNotFireEventTwice()
        {
            WireEvent();
            int count = 0;
            _onUnlocked.RegisterCallback(() => count++);

            _so.Unlock("ach_001");
            _so.Unlock("ach_001");

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Unlock_MultipleIds_AllRecorded()
        {
            _so.Unlock("ach_001");
            _so.Unlock("ach_002");
            _so.Unlock("ach_003");
            Assert.AreEqual(3, _so.UnlockedIds.Count);
            Assert.AreEqual("ach_003", _so.LastUnlockedId);
        }

        // ── RecordMatchResult ─────────────────────────────────────────────────

        [Test]
        public void RecordMatchResult_Win_IncrementsTotalMatchesPlayed()
        {
            _so.RecordMatchResult(playerWon: true);
            Assert.AreEqual(1, _so.TotalMatchesPlayed);
        }

        [Test]
        public void RecordMatchResult_Win_IncrementsTotalMatchesWon()
        {
            _so.RecordMatchResult(playerWon: true);
            Assert.AreEqual(1, _so.TotalMatchesWon);
        }

        [Test]
        public void RecordMatchResult_Loss_IncrementsTotalMatchesPlayed()
        {
            _so.RecordMatchResult(playerWon: false);
            Assert.AreEqual(1, _so.TotalMatchesPlayed);
        }

        [Test]
        public void RecordMatchResult_Loss_DoesNotIncrementWon()
        {
            _so.RecordMatchResult(playerWon: false);
            Assert.AreEqual(0, _so.TotalMatchesWon);
        }

        [Test]
        public void RecordMatchResult_Multiple_AccumulatesCorrectly()
        {
            _so.RecordMatchResult(playerWon: true);
            _so.RecordMatchResult(playerWon: false);
            _so.RecordMatchResult(playerWon: true);
            Assert.AreEqual(3, _so.TotalMatchesPlayed);
            Assert.AreEqual(2, _so.TotalMatchesWon);
        }

        // ── LoadSnapshot ──────────────────────────────────────────────────────

        [Test]
        public void LoadSnapshot_SetsMatchesPlayed()
        {
            _so.LoadSnapshot(7, 3, null);
            Assert.AreEqual(7, _so.TotalMatchesPlayed);
        }

        [Test]
        public void LoadSnapshot_SetsMatchesWon()
        {
            _so.LoadSnapshot(7, 3, null);
            Assert.AreEqual(3, _so.TotalMatchesWon);
        }

        [Test]
        public void LoadSnapshot_NegativeMatchesPlayed_ClampsToZero()
        {
            _so.LoadSnapshot(-5, -2, null);
            Assert.AreEqual(0, _so.TotalMatchesPlayed);
            Assert.AreEqual(0, _so.TotalMatchesWon);
        }

        [Test]
        public void LoadSnapshot_NullList_LeavesUnlockedEmpty()
        {
            _so.LoadSnapshot(0, 0, null);
            Assert.AreEqual(0, _so.UnlockedIds.Count);
        }

        [Test]
        public void LoadSnapshot_RestoresUnlockedIds()
        {
            var ids = new List<string> { "ach_001", "ach_002" };
            _so.LoadSnapshot(5, 3, ids);
            Assert.AreEqual(2, _so.UnlockedIds.Count);
            Assert.IsTrue(_so.HasUnlocked("ach_001"));
            Assert.IsTrue(_so.HasUnlocked("ach_002"));
        }

        [Test]
        public void LoadSnapshot_DuplicateIdsDeduped()
        {
            var ids = new List<string> { "ach_001", "ach_001" };
            _so.LoadSnapshot(1, 1, ids);
            Assert.AreEqual(1, _so.UnlockedIds.Count);
        }

        [Test]
        public void LoadSnapshot_DoesNotFireEvent()
        {
            WireEvent();
            int count = 0;
            _onUnlocked.RegisterCallback(() => count++);

            _so.LoadSnapshot(5, 3, new List<string> { "ach_001" });

            Assert.AreEqual(0, count);
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Test]
        public void Reset_ClearsCounters()
        {
            _so.RecordMatchResult(playerWon: true);
            _so.RecordMatchResult(playerWon: true);
            _so.Reset();
            Assert.AreEqual(0, _so.TotalMatchesPlayed);
            Assert.AreEqual(0, _so.TotalMatchesWon);
        }

        [Test]
        public void Reset_ClearsUnlockedIds()
        {
            _so.Unlock("ach_001");
            _so.Reset();
            Assert.AreEqual(0, _so.UnlockedIds.Count);
            Assert.IsFalse(_so.HasUnlocked("ach_001"));
        }

        [Test]
        public void Reset_ClearsLastUnlockedId()
        {
            _so.Unlock("ach_001");
            _so.Reset();
            Assert.IsNull(_so.LastUnlockedId);
        }

        [Test]
        public void Reset_DoesNotFireEvent()
        {
            WireEvent();
            _so.Unlock("ach_001");
            int count = 0;
            _onUnlocked.RegisterCallback(() => count++);

            _so.Reset();

            Assert.AreEqual(0, count);
        }
    }
}
