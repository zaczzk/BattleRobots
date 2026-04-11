using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="TournamentManager"/>.
    ///
    /// Covers:
    ///   • StartTournament() — null-state no-throw; deducts entry fee; activates tournament;
    ///     already-active guard (no double-start).
    ///   • HandleMatchEnded() — null-state no-throw; not-active no-throw;
    ///     null-result treated as loss; win advances round + awards bonus;
    ///     win completing all rounds awards grand prize + ends tournament;
    ///     loss eliminates player + awards consolation prize.
    ///   • OnDisable unregisters from _onMatchEnded channel.
    ///
    /// TournamentManager is a MonoBehaviour; the GameObject is created inactive so
    /// fields can be wired before Awake/OnEnable run.  Activate() triggers both.
    /// </summary>
    public class TournamentManagerTests
    {
        // ── Scene objects ─────────────────────────────────────────────────────

        private GameObject        _go;
        private TournamentManager _manager;

        // ── ScriptableObjects ─────────────────────────────────────────────────

        private TournamentStateSO _state;
        private TournamentConfig  _config;
        private PlayerWallet      _wallet;
        private MatchResultSO     _matchResult;
        private VoidGameEvent     _onMatchEnded;
        private VoidGameEvent     _onStarted;
        private VoidGameEvent     _onAdvanced;
        private VoidGameEvent     _onEnded;
        private IntGameEvent      _balanceEvent;

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetField(object target, string name, object value)
        {
            FieldInfo fi = target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fi, $"Field '{name}' not found on {target.GetType().Name}.");
            fi.SetValue(target, value);
        }

        /// <summary>Wires all SO fields into the manager via reflection.</summary>
        private void WireManager()
        {
            SetField(_manager, "_tournament",   _state);
            SetField(_manager, "_config",       _config);
            SetField(_manager, "_wallet",       _wallet);
            SetField(_manager, "_matchResult",  _matchResult);
            SetField(_manager, "_onMatchEnded", _onMatchEnded);
        }

        /// <summary>Activates the GameObject, triggering Awake then OnEnable.</summary>
        private void Activate() => _go.SetActive(true);

        /// <summary>Writes a match result to the MatchResultSO blackboard.</summary>
        private void SetMatchResult(bool playerWon)
        {
            _matchResult.Write(playerWon, durationSeconds: 30f,
                               currencyEarned: 100, newWalletBalance: 600);
        }

        [SetUp]
        public void SetUp()
        {
            // Create the GameObject INACTIVE so fields can be injected before Awake.
            _go = new GameObject("TournamentManagerHost");
            _go.SetActive(false);
            _manager = _go.AddComponent<TournamentManager>();

            // TournamentStateSO with event channels
            _onStarted  = ScriptableObject.CreateInstance<VoidGameEvent>();
            _onAdvanced = ScriptableObject.CreateInstance<VoidGameEvent>();
            _onEnded    = ScriptableObject.CreateInstance<VoidGameEvent>();
            _state      = ScriptableObject.CreateInstance<TournamentStateSO>();
            SetField(_state, "_onTournamentStarted", _onStarted);
            SetField(_state, "_onRoundAdvanced",     _onAdvanced);
            SetField(_state, "_onTournamentEnded",   _onEnded);

            // TournamentConfig: 3 rounds, entry 100, round bonus 50, grand 500, consolation 25
            _config = ScriptableObject.CreateInstance<TournamentConfig>();
            SetField(_config, "_roundCount",       3);
            SetField(_config, "_entryFee",         100);
            SetField(_config, "_roundWinBonus",    50);
            SetField(_config, "_grandPrize",       500);
            SetField(_config, "_consolationPrize", 25);

            // PlayerWallet with starting balance 1000
            _balanceEvent = ScriptableObject.CreateInstance<IntGameEvent>();
            _wallet       = ScriptableObject.CreateInstance<PlayerWallet>();
            SetField(_wallet, "_startingBalance",  1000);
            SetField(_wallet, "_onBalanceChanged", _balanceEvent);
            _wallet.Reset();

            _matchResult  = ScriptableObject.CreateInstance<MatchResultSO>();
            _onMatchEnded = ScriptableObject.CreateInstance<VoidGameEvent>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_state);
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_wallet);
            Object.DestroyImmediate(_matchResult);
            Object.DestroyImmediate(_onMatchEnded);
            Object.DestroyImmediate(_onStarted);
            Object.DestroyImmediate(_onAdvanced);
            Object.DestroyImmediate(_onEnded);
            Object.DestroyImmediate(_balanceEvent);
        }

        // ── StartTournament() ─────────────────────────────────────────────────

        [Test]
        public void StartTournament_NullState_DoesNotThrow()
        {
            // Only wire config and wallet — leave _tournament null
            SetField(_manager, "_config", _config);
            SetField(_manager, "_wallet", _wallet);
            Activate();

            Assert.DoesNotThrow(() => _manager.StartTournament());
        }

        [Test]
        public void StartTournament_DeductsEntryFee_FromWallet()
        {
            WireManager();
            Activate();
            int balanceBefore = _wallet.Balance; // 1000

            _manager.StartTournament();

            Assert.AreEqual(balanceBefore - _config.EntryFee, _wallet.Balance);
        }

        [Test]
        public void StartTournament_ActivatesTournamentState()
        {
            WireManager();
            Activate();

            _manager.StartTournament();

            Assert.IsTrue(_state.IsActive);
        }

        [Test]
        public void StartTournament_WhenAlreadyActive_DoesNotRestartOrDoubleDeduct()
        {
            WireManager();
            Activate();
            _manager.StartTournament(); // first enter — deducts 100
            int balanceAfterFirst = _wallet.Balance;

            _manager.StartTournament(); // guard: tournament already active — no-op

            Assert.AreEqual(balanceAfterFirst, _wallet.Balance,
                "Entry fee must not be deducted again when tournament is already active.");
            Assert.AreEqual(1, _state.CurrentRound,
                "CurrentRound must stay at 1 (no double-start).");
        }

        // ── HandleMatchEnded() ────────────────────────────────────────────────

        [Test]
        public void HandleMatchEnded_NullState_DoesNotThrow()
        {
            SetField(_manager, "_config",      _config);
            SetField(_manager, "_wallet",      _wallet);
            SetField(_manager, "_matchResult", _matchResult);
            Activate();

            Assert.DoesNotThrow(() => _manager.HandleMatchEnded());
        }

        [Test]
        public void HandleMatchEnded_TournamentNotActive_IsNoOp()
        {
            WireManager();
            Activate();
            // Do NOT call StartTournament — state not active
            int balanceBefore = _wallet.Balance;

            _manager.HandleMatchEnded();

            Assert.AreEqual(balanceBefore, _wallet.Balance,
                "Wallet must not change when tournament is not active.");
        }

        [Test]
        public void HandleMatchEnded_NullMatchResult_TreatedAsLoss()
        {
            WireManager();
            SetField(_manager, "_matchResult", null); // no result SO
            Activate();
            _manager.StartTournament();

            _manager.HandleMatchEnded(); // null result → playerWon = false

            Assert.IsTrue(_state.IsEliminated,
                "Null MatchResultSO should be treated as a loss (playerWon=false).");
        }

        [Test]
        public void HandleMatchEnded_Win_AdvancesRoundsWon()
        {
            WireManager();
            Activate();
            _manager.StartTournament();
            SetMatchResult(playerWon: true);

            _manager.HandleMatchEnded();

            Assert.AreEqual(1, _state.RoundsWon);
        }

        [Test]
        public void HandleMatchEnded_Win_AwardsRoundWinBonus()
        {
            WireManager();
            Activate();
            _manager.StartTournament();
            int balanceAfterEntry = _wallet.Balance;
            SetMatchResult(playerWon: true);

            _manager.HandleMatchEnded();

            Assert.AreEqual(balanceAfterEntry + _config.RoundWinBonus, _wallet.Balance,
                "RoundWinBonus must be credited on each round win.");
        }

        [Test]
        public void HandleMatchEnded_AllRoundsWon_EndsTournament()
        {
            WireManager();
            Activate();
            _manager.StartTournament();

            // Win all 3 rounds
            for (int i = 0; i < _config.RoundCount; i++)
            {
                SetMatchResult(playerWon: true);
                _manager.HandleMatchEnded();
            }

            Assert.IsFalse(_state.IsActive,
                "Tournament must not be active after all rounds are won.");
            Assert.AreEqual(_config.RoundCount, _state.RoundsWon,
                "All rounds must be recorded as won.");
        }

        [Test]
        public void HandleMatchEnded_AllRoundsWon_WalletIncludesGrandPrize()
        {
            WireManager();
            Activate();
            _manager.StartTournament();
            int balanceAfterEntry = _wallet.Balance;

            for (int i = 0; i < _config.RoundCount; i++)
            {
                SetMatchResult(playerWon: true);
                _manager.HandleMatchEnded();
            }

            // Each round win: +RoundWinBonus; final trigger: +GrandPrize
            int expected = balanceAfterEntry
                + _config.RoundWinBonus * _config.RoundCount
                + _config.GrandPrize;
            Assert.AreEqual(expected, _wallet.Balance,
                "Wallet must include all round bonuses and the grand prize.");
        }

        [Test]
        public void HandleMatchEnded_Loss_EliminatesPlayer()
        {
            WireManager();
            Activate();
            _manager.StartTournament();
            SetMatchResult(playerWon: false);

            _manager.HandleMatchEnded();

            Assert.IsTrue(_state.IsEliminated, "Player must be eliminated after a loss.");
        }

        [Test]
        public void HandleMatchEnded_Loss_AwardsConsolationPrize()
        {
            WireManager();
            Activate();
            _manager.StartTournament();
            int balanceAfterEntry = _wallet.Balance;
            SetMatchResult(playerWon: false);

            _manager.HandleMatchEnded();

            Assert.AreEqual(balanceAfterEntry + _config.ConsolationPrize, _wallet.Balance,
                "ConsolationPrize must be credited after elimination.");
        }

        // ── OnDisable unregisters ─────────────────────────────────────────────

        [Test]
        public void OnDisable_UnregistersFromMatchEndedChannel()
        {
            WireManager();
            Activate(); // Awake + OnEnable — registers _handleMatchEndedDelegate

            _manager.StartTournament();
            _go.SetActive(false); // OnDisable — unregisters

            // State is eliminated and inactive, so re-start to give handler something to do
            _state.Reset();
            _state.StartTournament();

            int roundsBefore = _state.RoundsWon;
            SetMatchResult(playerWon: true);
            _onMatchEnded.Raise(); // should NOT call HandleMatchEnded (unregistered)

            Assert.AreEqual(roundsBefore, _state.RoundsWon,
                "HandleMatchEnded must not be called after OnDisable.");
        }
    }
}
