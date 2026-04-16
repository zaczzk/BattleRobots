using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that drives the zone-control tournament bracket HUD.
    /// Subscribes to match-end events, submits round results to the
    /// <see cref="ZoneControlTournamentSO"/>, and refreshes the display.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _roundLabel    → "Round: N/M".
    ///   _winsLabel     → "Wins: N".
    ///   _lossesLabel   → "Losses: N".
    ///   _targetLabel   → "Target: N zones"  /  "Tournament Complete!".
    ///   _completeBadge → Active when <see cref="ZoneControlTournamentSO.IsComplete"/>.
    ///   _panel         → Root panel; hidden when <c>_tournamentSO</c> is null.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one tournament panel per HUD.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _tournamentSO       → ZoneControlTournamentSO asset.
    ///   2. Assign _summarySO          → ZoneControlSessionSummarySO asset.
    ///   3. Assign _onMatchEnded       → shared MatchEnded VoidGameEvent.
    ///   4. Assign _onRoundComplete    → tournamentSO._onRoundComplete channel.
    ///   5. Assign _onTournamentComplete → tournamentSO._onTournamentComplete channel.
    ///   6. Assign label / badge / panel UI refs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlTournamentController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlTournamentSO     _tournamentSO;
        [SerializeField] private ZoneControlSessionSummarySO _summarySO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Wire to the shared MatchEnded VoidGameEvent.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Tooltip("Wire to ZoneControlTournamentSO._onRoundComplete.")]
        [SerializeField] private VoidGameEvent _onRoundComplete;

        [Tooltip("Wire to ZoneControlTournamentSO._onTournamentComplete.")]
        [SerializeField] private VoidGameEvent _onTournamentComplete;

        [Header("UI Refs — Labels (optional)")]
        [SerializeField] private Text _roundLabel;
        [SerializeField] private Text _winsLabel;
        [SerializeField] private Text _lossesLabel;
        [SerializeField] private Text _targetLabel;

        [Header("UI Refs — Badge / Panel (optional)")]
        [SerializeField] private GameObject _completeBadge;
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleMatchEndedDelegate;
        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleMatchEndedDelegate = HandleMatchEnded;
            _refreshDelegate          = Refresh;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onRoundComplete?.RegisterCallback(_refreshDelegate);
            _onTournamentComplete?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onRoundComplete?.UnregisterCallback(_refreshDelegate);
            _onTournamentComplete?.UnregisterCallback(_refreshDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Reads current match results from the summary SO, submits them to the
        /// tournament SO, then refreshes the HUD.
        /// No-op when <c>_tournamentSO</c> is null.
        /// </summary>
        public void HandleMatchEnded()
        {
            if (_tournamentSO == null)
            {
                Refresh();
                return;
            }

            int target = _tournamentSO.CurrentRoundTarget;
            if (target < 0)
            {
                Refresh();
                return;
            }

            int zonesCaptured = _summarySO != null ? _summarySO.TotalZonesCaptured : 0;
            _tournamentSO.SubmitRoundResult(zonesCaptured, target);
            Refresh();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds all HUD elements from the current tournament SO state.
        /// Hides the panel when <c>_tournamentSO</c> is null.
        /// </summary>
        public void Refresh()
        {
            if (_tournamentSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_roundLabel != null)
                _roundLabel.text = $"Round: {_tournamentSO.CurrentRound}/{_tournamentSO.RoundCount}";

            if (_winsLabel != null)
                _winsLabel.text = $"Wins: {_tournamentSO.RoundWins}";

            if (_lossesLabel != null)
                _lossesLabel.text = $"Losses: {_tournamentSO.RoundLosses}";

            if (_targetLabel != null)
            {
                int target = _tournamentSO.CurrentRoundTarget;
                _targetLabel.text = target >= 0
                    ? $"Target: {target} zones"
                    : "Tournament Complete!";
            }

            _completeBadge?.SetActive(_tournamentSO.IsComplete);
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound tournament SO (may be null).</summary>
        public ZoneControlTournamentSO TournamentSO => _tournamentSO;

        /// <summary>The bound session summary SO (may be null).</summary>
        public ZoneControlSessionSummarySO SummarySO => _summarySO;
    }
}
