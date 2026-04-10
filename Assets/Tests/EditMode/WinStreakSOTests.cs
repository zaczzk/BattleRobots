using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="WinStreakSO"/>.
    ///
    /// Covers:
    ///   • Fresh-instance defaults (CurrentStreak=0, BestStreak=0).
    ///   • <see cref="WinStreakSO.RecordWin"/>: increments CurrentStreak, updates
    ///     BestStreak only when a new high is set, fires _onStreakChanged.
    ///   • <see cref="WinStreakSO.RecordLoss"/>: resets CurrentStreak to 0, does
    ///     NOT reduce BestStreak, fires _onStreakChanged.
    ///   • <see cref="WinStreakSO.LoadSnapshot"/>: silent rehydration (no event),
    ///     negative values clamped to 0.
    ///   • <see cref="WinStreakSO.Reset"/>: silent clear of both fields (no event).
    ///   • Multi-call sequence: win×3 then loss gives streak=0 + best=3.
    ///   • RecordLoss when already at zero: idempotent, no throw.
    /// </summary>
    public class WinStreakSOTests
    {
        private WinStreakSO  _so;
        private VoidGameEvent _onChanged;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private void WireEvent()
        {
            SetField(_so, "_onStreakChanged", _onChanged);
        }

        // ── Setup / Teardown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            _so        = ScriptableObject.CreateInstance<WinStreakSO>();
            _onChanged = ScriptableObject.CreateInstance<VoidGameEvent>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_so);
            Object.DestroyImmediate(_onChanged);
            _so        = null;
            _onChanged = null;
        }

        // ── Fresh instance ────────────────────────────────────────────────────

        [Test]
        public void FreshInstance_CurrentStreak_IsZero()
        {
            Assert.AreEqual(0, _so.CurrentStreak);
        }

        [Test]
        public void FreshInstance_BestStreak_IsZero()
        {
            Assert.AreEqual(0, _so.BestStreak);
        }

        // ── RecordWin() ───────────────────────────────────────────────────────

        [Test]
        public void RecordWin_IncrementsCurrent_FromZero()
        {
            _so.RecordWin();
            Assert.AreEqual(1, _so.CurrentStreak);
        }

        [Test]
        public void RecordWin_UpdatesBestStreak_WhenNewHigh()
        {
            _so.RecordWin(); // streak = 1, best = 1
            _so.RecordWin(); // streak = 2, best = 2
            Assert.AreEqual(2, _so.BestStreak);
        }

        [Test]
        public void RecordWin_DoesNotUpdateBest_WhenLowerThanCurrent()
        {
            // Get best to 3, then lose, then win once — best stays at 3.
            _so.RecordWin(); _so.RecordWin(); _so.RecordWin(); // streak=3, best=3
            _so.RecordLoss();                                   // streak=0, best=3
            _so.RecordWin();                                    // streak=1, best still 3
            Assert.AreEqual(3, _so.BestStreak);
            Assert.AreEqual(1, _so.CurrentStreak);
        }

        [Test]
        public void RecordWin_WithNoEventWired_DoesNotThrow()
        {
            // _onStreakChanged left null — must be null-safe.
            Assert.DoesNotThrow(() => _so.RecordWin());
        }

        [Test]
        public void RecordWin_FiresOnStreakChangedEvent()
        {
            WireEvent();
            int fireCount = 0;
            _onChanged.RegisterCallback(() => fireCount++);

            _so.RecordWin();

            Assert.AreEqual(1, fireCount, "RecordWin() must raise _onStreakChanged.");
        }

        // ── RecordLoss() ──────────────────────────────────────────────────────

        [Test]
        public void RecordLoss_ResetsCurrent_ToZero()
        {
            _so.RecordWin(); _so.RecordWin(); // streak = 2
            _so.RecordLoss();
            Assert.AreEqual(0, _so.CurrentStreak);
        }

        [Test]
        public void RecordLoss_DoesNotReduceBestStreak()
        {
            _so.RecordWin(); _so.RecordWin(); // best = 2
            _so.RecordLoss();
            Assert.AreEqual(2, _so.BestStreak);
        }

        [Test]
        public void RecordLoss_WhenAlreadyZero_Idempotent_DoesNotThrow()
        {
            // CurrentStreak already 0.
            Assert.DoesNotThrow(() => _so.RecordLoss());
            Assert.AreEqual(0, _so.CurrentStreak);
        }

        [Test]
        public void RecordLoss_FiresOnStreakChangedEvent()
        {
            WireEvent();
            int fireCount = 0;
            _onChanged.RegisterCallback(() => fireCount++);

            _so.RecordLoss();

            Assert.AreEqual(1, fireCount, "RecordLoss() must raise _onStreakChanged.");
        }

        // ── LoadSnapshot() ────────────────────────────────────────────────────

        [Test]
        public void LoadSnapshot_SetsBothFields()
        {
            _so.LoadSnapshot(currentStreak: 4, bestStreak: 7);
            Assert.AreEqual(4, _so.CurrentStreak);
            Assert.AreEqual(7, _so.BestStreak);
        }

        [Test]
        public void LoadSnapshot_NegativeValues_ClampedToZero()
        {
            _so.LoadSnapshot(currentStreak: -3, bestStreak: -1);
            Assert.AreEqual(0, _so.CurrentStreak);
            Assert.AreEqual(0, _so.BestStreak);
        }

        [Test]
        public void LoadSnapshot_DoesNotFireChangedEvent()
        {
            WireEvent();
            int fireCount = 0;
            _onChanged.RegisterCallback(() => fireCount++);

            _so.LoadSnapshot(currentStreak: 5, bestStreak: 10);

            Assert.AreEqual(0, fireCount, "LoadSnapshot() must not fire _onStreakChanged.");
        }

        // ── Reset() ───────────────────────────────────────────────────────────

        [Test]
        public void Reset_ZeroesBothFields()
        {
            _so.RecordWin(); _so.RecordWin(); // streak=2, best=2
            _so.Reset();
            Assert.AreEqual(0, _so.CurrentStreak);
            Assert.AreEqual(0, _so.BestStreak);
        }

        [Test]
        public void Reset_DoesNotFireChangedEvent()
        {
            WireEvent();
            int fireCount = 0;
            _onChanged.RegisterCallback(() => fireCount++);

            _so.RecordWin(); // count=1
            _so.Reset();     // must NOT fire

            Assert.AreEqual(1, fireCount, "Reset() must not fire _onStreakChanged.");
        }

        // ── Multi-call sequence ───────────────────────────────────────────────

        [Test]
        public void MultiWinThenLoss_StreakResetsButBestPreserved()
        {
            _so.RecordWin(); // streak=1, best=1
            _so.RecordWin(); // streak=2, best=2
            _so.RecordWin(); // streak=3, best=3
            _so.RecordLoss();// streak=0, best=3

            Assert.AreEqual(0, _so.CurrentStreak, "CurrentStreak must reset after loss.");
            Assert.AreEqual(3, _so.BestStreak,    "BestStreak must survive the loss.");
        }
    }
}
