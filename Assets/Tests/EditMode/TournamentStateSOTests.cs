using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="TournamentStateSO"/>.
    ///
    /// Covers:
    ///   • Fresh-instance defaults (IsActive=false, CurrentRound=0, RoundsWon=0, IsEliminated=false).
    ///   • <see cref="TournamentStateSO.StartTournament"/>: sets active, initialises round to 1,
    ///     clears elimination, fires _onTournamentStarted.
    ///   • <see cref="TournamentStateSO.RecordRoundResult"/> (win): increments RoundsWon and
    ///     CurrentRound, fires _onRoundAdvanced. Does NOT call EndTournament automatically.
    ///   • <see cref="TournamentStateSO.RecordRoundResult"/> (loss): sets IsEliminated=true,
    ///     clears IsActive, fires _onRoundAdvanced.
    ///   • <see cref="TournamentStateSO.IsTournamentWon"/>: returns false below threshold,
    ///     true at threshold, false for totalRounds ≤ 0.
    ///   • <see cref="TournamentStateSO.EndTournament"/>: clears IsActive, fires _onTournamentEnded.
    ///   • <see cref="TournamentStateSO.Reset"/>: silently zeros all fields without firing events.
    ///   • Null event channels are handled without throwing.
    /// </summary>
    public class TournamentStateSOTests
    {
        private TournamentStateSO _state;
        private VoidGameEvent     _onStarted;
        private VoidGameEvent     _onAdvanced;
        private VoidGameEvent     _onEnded;

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        private void WireEvents()
        {
            SetField(_state, "_onTournamentStarted", _onStarted);
            SetField(_state, "_onRoundAdvanced",     _onAdvanced);
            SetField(_state, "_onTournamentEnded",   _onEnded);
        }

        [SetUp]
        public void SetUp()
        {
            _state      = ScriptableObject.CreateInstance<TournamentStateSO>();
            _onStarted  = ScriptableObject.CreateInstance<VoidGameEvent>();
            _onAdvanced = ScriptableObject.CreateInstance<VoidGameEvent>();
            _onEnded    = ScriptableObject.CreateInstance<VoidGameEvent>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_state);
            Object.DestroyImmediate(_onStarted);
            Object.DestroyImmediate(_onAdvanced);
            Object.DestroyImmediate(_onEnded);
        }

        // ── Fresh-instance defaults ───────────────────────────────────────────

        [Test]
        public void FreshInstance_IsActive_IsFalse()
        {
            Assert.IsFalse(_state.IsActive);
        }

        [Test]
        public void FreshInstance_CurrentRound_IsZero()
        {
            Assert.AreEqual(0, _state.CurrentRound);
        }

        [Test]
        public void FreshInstance_RoundsWon_IsZero()
        {
            Assert.AreEqual(0, _state.RoundsWon);
        }

        [Test]
        public void FreshInstance_IsEliminated_IsFalse()
        {
            Assert.IsFalse(_state.IsEliminated);
        }

        // ── StartTournament() ─────────────────────────────────────────────────

        [Test]
        public void StartTournament_SetsIsActive_True()
        {
            _state.StartTournament();
            Assert.IsTrue(_state.IsActive);
        }

        [Test]
        public void StartTournament_SetsCurrentRound_ToOne()
        {
            _state.StartTournament();
            Assert.AreEqual(1, _state.CurrentRound);
        }

        [Test]
        public void StartTournament_ClearsElimination()
        {
            // Simulate prior elimination then restart
            _state.StartTournament();
            _state.RecordRoundResult(playerWon: false); // eliminated
            Assert.IsTrue(_state.IsEliminated);

            _state.StartTournament(); // restart
            Assert.IsFalse(_state.IsEliminated);
        }

        [Test]
        public void StartTournament_FiresOnTournamentStarted()
        {
            WireEvents();
            int count = 0;
            _onStarted.RegisterCallback(() => count++);

            _state.StartTournament();

            Assert.AreEqual(1, count);
        }

        [Test]
        public void StartTournament_NullEventChannels_DoesNotThrow()
        {
            // No WireEvents() — channels remain null
            Assert.DoesNotThrow(() => _state.StartTournament());
        }

        // ── RecordRoundResult() — win ─────────────────────────────────────────

        [Test]
        public void RecordRoundResult_Win_IncrementsRoundsWon()
        {
            _state.StartTournament();
            _state.RecordRoundResult(playerWon: true);
            Assert.AreEqual(1, _state.RoundsWon);
        }

        [Test]
        public void RecordRoundResult_Win_IncrementsCurrentRound()
        {
            _state.StartTournament();
            _state.RecordRoundResult(playerWon: true);
            Assert.AreEqual(2, _state.CurrentRound);
        }

        [Test]
        public void RecordRoundResult_Win_KeepsIsActive_True()
        {
            _state.StartTournament();
            _state.RecordRoundResult(playerWon: true);
            Assert.IsTrue(_state.IsActive);
        }

        [Test]
        public void RecordRoundResult_Win_FiresOnRoundAdvanced()
        {
            WireEvents();
            _state.StartTournament();
            int count = 0;
            _onAdvanced.RegisterCallback(() => count++);

            _state.RecordRoundResult(playerWon: true);

            Assert.AreEqual(1, count);
        }

        // ── RecordRoundResult() — loss ────────────────────────────────────────

        [Test]
        public void RecordRoundResult_Loss_SetsIsEliminated()
        {
            _state.StartTournament();
            _state.RecordRoundResult(playerWon: false);
            Assert.IsTrue(_state.IsEliminated);
        }

        [Test]
        public void RecordRoundResult_Loss_ClearsIsActive()
        {
            _state.StartTournament();
            _state.RecordRoundResult(playerWon: false);
            Assert.IsFalse(_state.IsActive);
        }

        [Test]
        public void RecordRoundResult_Loss_FiresOnRoundAdvanced()
        {
            WireEvents();
            _state.StartTournament();
            int count = 0;
            _onAdvanced.RegisterCallback(() => count++);

            _state.RecordRoundResult(playerWon: false);

            Assert.AreEqual(1, count);
        }

        // ── IsTournamentWon() ─────────────────────────────────────────────────

        [Test]
        public void IsTournamentWon_BelowThreshold_ReturnsFalse()
        {
            _state.StartTournament();
            _state.RecordRoundResult(playerWon: true); // roundsWon = 1
            Assert.IsFalse(_state.IsTournamentWon(totalRounds: 3));
        }

        [Test]
        public void IsTournamentWon_AtThreshold_ReturnsTrue()
        {
            _state.StartTournament();
            _state.RecordRoundResult(playerWon: true); // 1
            _state.RecordRoundResult(playerWon: true); // 2
            _state.RecordRoundResult(playerWon: true); // 3
            Assert.IsTrue(_state.IsTournamentWon(totalRounds: 3));
        }

        [Test]
        public void IsTournamentWon_ZeroTotalRounds_ReturnsFalse()
        {
            // totalRounds ≤ 0 is a degenerate case — should return false
            Assert.IsFalse(_state.IsTournamentWon(totalRounds: 0));
        }

        // ── EndTournament() ───────────────────────────────────────────────────

        [Test]
        public void EndTournament_ClearsIsActive()
        {
            _state.StartTournament();
            _state.EndTournament();
            Assert.IsFalse(_state.IsActive);
        }

        [Test]
        public void EndTournament_FiresOnTournamentEnded()
        {
            WireEvents();
            _state.StartTournament();
            int count = 0;
            _onEnded.RegisterCallback(() => count++);

            _state.EndTournament();

            Assert.AreEqual(1, count);
        }

        // ── Reset() ───────────────────────────────────────────────────────────

        [Test]
        public void Reset_ZeroesAllFields()
        {
            _state.StartTournament();
            _state.RecordRoundResult(playerWon: true);
            _state.Reset();

            Assert.IsFalse(_state.IsActive,    "IsActive must be false after Reset.");
            Assert.AreEqual(0, _state.CurrentRound, "CurrentRound must be 0 after Reset.");
            Assert.AreEqual(0, _state.RoundsWon,    "RoundsWon must be 0 after Reset.");
            Assert.IsFalse(_state.IsEliminated, "IsEliminated must be false after Reset.");
        }

        [Test]
        public void Reset_DoesNotFireAnyEvent()
        {
            WireEvents();
            _state.StartTournament();

            int startedCount  = 0;
            int advancedCount = 0;
            int endedCount    = 0;
            _onStarted.RegisterCallback(()  => startedCount++);
            _onAdvanced.RegisterCallback(() => advancedCount++);
            _onEnded.RegisterCallback(()    => endedCount++);

            _state.Reset();

            Assert.AreEqual(0, startedCount,  "Reset() must not fire _onTournamentStarted.");
            Assert.AreEqual(0, advancedCount, "Reset() must not fire _onRoundAdvanced.");
            Assert.AreEqual(0, endedCount,    "Reset() must not fire _onTournamentEnded.");
        }
    }
}
