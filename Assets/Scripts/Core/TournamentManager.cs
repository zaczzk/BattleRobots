using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Drives the multi-round tournament loop: entering, advancing after each match,
    /// awarding per-round bonuses, and distributing the grand prize or consolation
    /// prize on completion.
    ///
    /// ── Integration with MatchManager ────────────────────────────────────────
    ///   TournamentManager subscribes to the same <c>_onMatchEnded</c> VoidGameEvent
    ///   that MatchManager raises.  It reads the match outcome from the
    ///   <see cref="MatchResultSO"/> blackboard (written by MatchManager before
    ///   the event fires), so it always has access to up-to-date result data.
    ///
    /// ── Economy flow ─────────────────────────────────────────────────────────
    ///   1. <see cref="StartTournament"/>  — deducts <c>EntryFee</c> from wallet.
    ///   2. Each round win               — credits <c>RoundWinBonus</c>.
    ///   3. All rounds won               — credits <c>GrandPrize</c>, ends tournament.
    ///   4. Elimination                  — credits <c>ConsolationPrize</c> (may be 0).
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Cached System.Action delegate — no heap allocation after Awake.
    ///   - All inspector fields are optional and null-safe so the component can be
    ///     added to a scene without immediate wiring (backwards-compatible).
    ///   - <see cref="StartTournament"/> is a no-op when no tournament is active
    ///     and safe to call from a UI Button's onClick.
    ///
    /// Scene wiring:
    ///   • Add a VoidGameEventListener on the same GameObject:
    ///       Event = MatchEnded SO, Response = TournamentManager.HandleMatchEnded().
    ///   • Assign _tournament, _config, _wallet, _matchResult in the Inspector.
    /// </summary>
    public sealed class TournamentManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Tournament")]
        [Tooltip("Runtime state blackboard for the active tournament.")]
        [SerializeField] private TournamentStateSO _tournament;

        [Tooltip("Balance configuration: round count, fees, bonuses, prizes.")]
        [SerializeField] private TournamentConfig _config;

        [Header("Economy")]
        [Tooltip("Player wallet — entry fee deducted, bonuses/prizes credited.")]
        [SerializeField] private PlayerWallet _wallet;

        [Header("Match Result")]
        [Tooltip("Blackboard written by MatchManager before _onMatchEnded fires. " +
                 "TournamentManager reads PlayerWon from this SO.")]
        [SerializeField] private MatchResultSO _matchResult;

        [Header("Event Channels — In")]
        [Tooltip("Subscribe to the same MatchEnded VoidGameEvent used by MatchManager. " +
                 "Alternatively wire via a VoidGameEventListener on this GameObject.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Cached delegate ───────────────────────────────────────────────────

        private System.Action _handleMatchEndedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleMatchEndedDelegate = HandleMatchEnded;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to enter the player into a new tournament.
        /// Deducts the <see cref="TournamentConfig.EntryFee"/> from the wallet (if a
        /// wallet and config are assigned), then delegates to
        /// <see cref="TournamentStateSO.StartTournament"/>.
        ///
        /// No-op when:
        ///   • <c>_tournament</c> is null, or
        ///   • A tournament is already active (<see cref="TournamentStateSO.IsActive"/>).
        /// </summary>
        public void StartTournament()
        {
            if (_tournament == null) return;
            if (_tournament.IsActive) return;

            // Deduct entry fee — may leave wallet negative if designer set a high fee;
            // PlayerWallet.Deduct() clamps to 0 internally, so no crash risk.
            if (_wallet != null && _config != null && _config.EntryFee > 0)
                _wallet.Deduct(_config.EntryFee);

            _tournament.StartTournament();

            Debug.Log($"[TournamentManager] Tournament started. Entry fee: {_config?.EntryFee ?? 0}.");
        }

        /// <summary>
        /// Called when the MatchEnded VoidGameEvent fires (or wired via a
        /// VoidGameEventListener response).
        ///
        /// Reads the match outcome from <see cref="MatchResultSO.PlayerWon"/>,
        /// advances the tournament state, and distributes economy rewards.
        /// Silent no-op when no tournament is active.
        /// </summary>
        public void HandleMatchEnded()
        {
            if (_tournament == null || !_tournament.IsActive) return;
            if (_config == null) return;

            bool playerWon = _matchResult != null && _matchResult.PlayerWon;

            // Award per-round win bonus before updating state
            if (playerWon && _wallet != null && _config.RoundWinBonus > 0)
                _wallet.AddFunds(_config.RoundWinBonus);

            // Update state — RecordRoundResult fires _onRoundAdvanced internally
            _tournament.RecordRoundResult(playerWon);

            if (_tournament.IsTournamentWon(_config.RoundCount))
            {
                // Player has completed all rounds — award grand prize and end
                if (_wallet != null && _config.GrandPrize > 0)
                    _wallet.AddFunds(_config.GrandPrize);

                _tournament.EndTournament();

                Debug.Log($"[TournamentManager] Tournament WON! " +
                          $"Rounds: {_tournament.RoundsWon}/{_config.RoundCount}, " +
                          $"GrandPrize: {_config.GrandPrize}.");
            }
            else if (_tournament.IsEliminated)
            {
                // Player was knocked out — award consolation prize (may be 0)
                if (_wallet != null && _config.ConsolationPrize > 0)
                    _wallet.AddFunds(_config.ConsolationPrize);

                Debug.Log($"[TournamentManager] Tournament LOST. " +
                          $"Consolation: {_config.ConsolationPrize}.");
            }
            else
            {
                Debug.Log($"[TournamentManager] Round {_tournament.CurrentRound - 1} won. " +
                          $"RoundsWon={_tournament.RoundsWon}/{_config.RoundCount}.");
            }
        }
    }
}
