using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that records match results into a
    /// <see cref="ZoneControlProfileSO"/> and displays career profile statistics.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   On <c>_onMatchEnded</c>: reads <c>ZoneControlSessionSummarySO.TotalZonesCaptured</c>
    ///   and determines win/loss from <c>ZoneControlScoreboardSO.PlayerScore</c>;
    ///   calls <see cref="ZoneControlProfileSO.RecordMatchResult"/>, then refreshes.
    ///   On <c>_onProfileUpdated</c>: calls <see cref="Refresh"/>.
    ///   On <c>_onMatchStarted</c>: calls <see cref="Refresh"/> to reflect reset state.
    ///   <see cref="Refresh"/>: hides panel when SO null; shows "Wins: N", "Win Rate: N%",
    ///   "Avg Zones: F1" labels.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one profile controller per scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlProfileController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlProfileSO        _profileSO;
        [SerializeField] private ZoneControlScoreboardSO     _scoreboardSO;
        [SerializeField] private ZoneControlSessionSummarySO _summarySO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onProfileUpdated;
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _winsLabel;
        [SerializeField] private Text       _winRateLabel;
        [SerializeField] private Text       _avgZonesLabel;
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleMatchEndedDelegate;
        private Action _handleProfileUpdatedDelegate;
        private Action _handleMatchStartedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleMatchEndedDelegate    = HandleMatchEnded;
            _handleProfileUpdatedDelegate = Refresh;
            _handleMatchStartedDelegate  = HandleMatchStarted;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onProfileUpdated?.RegisterCallback(_handleProfileUpdatedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onProfileUpdated?.UnregisterCallback(_handleProfileUpdatedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>Records the match result into the profile SO and refreshes the display.</summary>
        public void HandleMatchEnded()
        {
            if (_profileSO != null)
            {
                bool playerWon = _scoreboardSO != null
                    && _scoreboardSO.PlayerScore > _scoreboardSO.GetBotScore(0);

                int zones = _summarySO != null ? _summarySO.TotalZonesCaptured : 0;

                _profileSO.RecordMatchResult(playerWon, zones);
            }
            Refresh();
        }

        /// <summary>Refreshes the display at match start to show current career stats.</summary>
        public void HandleMatchStarted() => Refresh();

        // ── Display ───────────────────────────────────────────────────────────

        /// <summary>
        /// Updates panel visibility and career stat labels.
        /// Hides the panel when the profile SO is null.
        /// </summary>
        public void Refresh()
        {
            if (_profileSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_winsLabel    != null) _winsLabel.text    = $"Wins: {_profileSO.TotalWins}";
            if (_winRateLabel != null) _winRateLabel.text = $"Win Rate: {_profileSO.WinRate * 100f:F0}%";
            if (_avgZonesLabel != null) _avgZonesLabel.text = $"Avg Zones: {_profileSO.AverageZonesPerMatch:F1}";
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound profile SO (may be null).</summary>
        public ZoneControlProfileSO ProfileSO => _profileSO;
    }
}
