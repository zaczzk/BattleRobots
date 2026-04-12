using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace BattleRobots.UI
{
    /// <summary>
    /// Drives the tournament lobby UI: displays current round progress, entry fee,
    /// and routes the Enter/Abandon button events to <see cref="BattleRobots.Core.TournamentManager"/>.
    ///
    /// ── What this controller does ─────────────────────────────────────────────
    ///   • Subscribes to <c>_onTournamentStarted</c>, <c>_onRoundAdvanced</c>, and
    ///     <c>_onTournamentEnded</c> VoidGameEvent channels.
    ///   • Calls <see cref="Refresh"/> on each event to update text labels.
    ///   • Wires the Enter Button's onClick in Awake (null-safe).
    ///   • Does NOT reference BattleRobots.Physics.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields are optional and null-safe.
    ///   - Cached UnityAction delegates — no heap allocation after Awake.
    ///   - No Update loop.
    ///
    /// Scene wiring:
    ///   • Add to any persistent Canvas GameObject in the pre-match lobby.
    ///   • Assign _tournamentState, _config, _tournamentManager, and event channels.
    ///   • Assign optional Text fields and the _enterButton.
    /// </summary>
    public sealed class TournamentController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Runtime tournament state SO — source of truth for UI display.")]
        [SerializeField] private Core.TournamentStateSO _tournamentState;

        [Tooltip("Balance config — provides round count and entry fee for display.")]
        [SerializeField] private Core.TournamentConfig _config;

        [Tooltip("Manager MB on any persistent scene object — called by Enter button.")]
        [SerializeField] private Core.TournamentManager _tournamentManager;

        [Header("Event Channels — In")]
        [Tooltip("Raised by TournamentStateSO when a tournament starts.")]
        [SerializeField] private Core.VoidGameEvent _onTournamentStarted;

        [Tooltip("Raised by TournamentStateSO after each round result is recorded.")]
        [SerializeField] private Core.VoidGameEvent _onRoundAdvanced;

        [Tooltip("Raised by TournamentStateSO when the tournament ends.")]
        [SerializeField] private Core.VoidGameEvent _onTournamentEnded;

        [Header("UI Labels (optional)")]
        [Tooltip("Shows the current round number, e.g. 'Round 2 / 3'.")]
        [SerializeField] private Text _roundText;

        [Tooltip("Shows the player's current wins, e.g. 'Wins: 1'.")]
        [SerializeField] private Text _winsText;

        [Tooltip("Shows the entry fee, e.g. 'Entry Fee: 100'.")]
        [SerializeField] private Text _entryFeeText;

        [Tooltip("Shows the grand prize amount, e.g. 'Grand Prize: 500'.")]
        [SerializeField] private Text _grandPrizeText;

        [Tooltip("Shows tournament status: 'Active', 'Eliminated', or 'Not Active'.")]
        [SerializeField] private Text _statusText;

        [Header("Buttons (optional)")]
        [Tooltip("Button that calls TournamentManager.StartTournament(). " +
                 "Interactable only when no tournament is active and tier/rating requirements are met.")]
        [SerializeField] private Button _enterButton;

        [Header("Tier Gating (optional)")]
        [Tooltip("Build rating SO — evaluated for tier and rating requirements. " +
                 "Leave null to skip gating display (Enter button enabled whenever inactive).")]
        [SerializeField] private Core.BuildRatingSO _buildRating;

        [Tooltip("Tier config SO — resolves tier display names. " +
                 "Leave null to fall back to enum names.")]
        [SerializeField] private Core.RobotTierConfig _tierConfig;

        [Tooltip("IntGameEvent raised by BuildRatingSO when the rating changes — " +
                 "triggers Refresh() so the lock state updates live.")]
        [SerializeField] private Core.IntGameEvent _onRatingChanged;

        [Tooltip("Text label shown only when the tournament is locked by tier/rating requirements. " +
                 "Hidden when unlocked or while a tournament is already active.")]
        [SerializeField] private Text _lockReasonText;

        // ── Cached delegates ──────────────────────────────────────────────────

        // System.Action for SO channel callbacks (VoidGameEvent.RegisterCallback)
        private Action      _refreshDelegate;
        // UnityAction for Button.onClick (UnityEvent<> API)
        private UnityAction _onEnterClickedDelegate;
        // System.Action<int> for IntGameEvent.RegisterCallback (rating-changed channel)
        private Action<int> _onRatingChangedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate        = Refresh;
            _onEnterClickedDelegate = OnEnterClicked;
            _onRatingChangedDelegate = _ => Refresh();

            _enterButton?.onClick.AddListener(_onEnterClickedDelegate);
        }

        private void OnEnable()
        {
            _onTournamentStarted?.RegisterCallback(_refreshDelegate);
            _onRoundAdvanced?.RegisterCallback(_refreshDelegate);
            _onTournamentEnded?.RegisterCallback(_refreshDelegate);
            _onRatingChanged?.RegisterCallback(_onRatingChangedDelegate);

            Refresh();
        }

        private void OnDisable()
        {
            _onTournamentStarted?.UnregisterCallback(_refreshDelegate);
            _onRoundAdvanced?.UnregisterCallback(_refreshDelegate);
            _onTournamentEnded?.UnregisterCallback(_refreshDelegate);
            _onRatingChanged?.UnregisterCallback(_onRatingChangedDelegate);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void Refresh()
        {
            bool active    = _tournamentState != null && _tournamentState.IsActive;
            int  roundCount = _config != null ? _config.RoundCount : 0;

            if (_roundText != null)
            {
                if (_tournamentState != null && active)
                    _roundText.text = $"Round {_tournamentState.CurrentRound} / {roundCount}";
                else
                    _roundText.text = "—";
            }

            if (_winsText != null)
            {
                _winsText.text = _tournamentState != null
                    ? $"Wins: {_tournamentState.RoundsWon}"
                    : "Wins: 0";
            }

            if (_entryFeeText != null)
            {
                _entryFeeText.text = _config != null
                    ? $"Entry Fee: {_config.EntryFee}"
                    : "Entry Fee: —";
            }

            if (_grandPrizeText != null)
            {
                _grandPrizeText.text = _config != null
                    ? $"Grand Prize: {_config.GrandPrize}"
                    : "Grand Prize: —";
            }

            if (_statusText != null)
            {
                if (_tournamentState == null)
                    _statusText.text = "Not Active";
                else if (_tournamentState.IsEliminated)
                    _statusText.text = "Eliminated";
                else if (active)
                    _statusText.text = "Active";
                else if (_tournamentState.IsTournamentWon(roundCount))
                    _statusText.text = "Victory!";
                else
                    _statusText.text = "Not Active";
            }

            // Tier / rating gate — null _buildRating → treat as unlocked (backwards-compat)
            bool isUnlocked = Core.TournamentGatingEvaluator.IsUnlocked(
                _config, _buildRating, _tierConfig);

            // Enter button: interactable only when inactive AND requirements met
            if (_enterButton != null)
                _enterButton.interactable = !active && isUnlocked;

            // Lock-reason label: shown only when not active and requirements not met
            if (_lockReasonText != null)
            {
                bool showLock = !active && !isUnlocked;
                _lockReasonText.gameObject.SetActive(showLock);
                if (showLock)
                    _lockReasonText.text = Core.TournamentGatingEvaluator.GetLockReason(
                        _config, _buildRating, _tierConfig);
            }
        }

        private void OnEnterClicked()
        {
            _tournamentManager?.StartTournament();
        }
    }
}
