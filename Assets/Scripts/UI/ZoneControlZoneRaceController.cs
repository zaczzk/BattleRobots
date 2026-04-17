using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that bridges zone-capture events into
    /// <see cref="ZoneControlZoneRaceSO"/> and displays the first-to-N race progress.
    ///
    /// <c>_onPlayerZoneCaptured</c>: adds a player capture + Refresh.
    /// <c>_onBotZoneCaptured</c>: adds a bot capture + Refresh.
    /// <c>_onMatchStarted</c>: resets the SO + Refresh.
    /// <c>_onPlayerWon</c> / <c>_onBotWon</c>: Refresh.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlZoneRaceController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlZoneRaceSO _raceSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPlayerWon;
        [SerializeField] private VoidGameEvent _onBotWon;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _playerLabel;
        [SerializeField] private Text       _botLabel;
        [SerializeField] private Text       _winnerLabel;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerCapturedDelegate;
        private Action _handleBotCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handlePlayerCapturedDelegate = HandlePlayerCapture;
            _handleBotCapturedDelegate    = HandleBotCapture;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _refreshDelegate              = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onPlayerWon?.RegisterCallback(_refreshDelegate);
            _onBotWon?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPlayerWon?.UnregisterCallback(_refreshDelegate);
            _onBotWon?.UnregisterCallback(_refreshDelegate);
        }

        private void HandlePlayerCapture()
        {
            _raceSO?.AddPlayerCapture();
            Refresh();
        }

        private void HandleBotCapture()
        {
            _raceSO?.AddBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _raceSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_raceSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_playerLabel != null)
                _playerLabel.text = $"Player: {_raceSO.PlayerCaptures}/{_raceSO.CaptureTarget}";

            if (_botLabel != null)
                _botLabel.text = $"Bot: {_raceSO.BotCaptures}/{_raceSO.CaptureTarget}";

            if (_winnerLabel != null)
            {
                if (!_raceSO.IsRaceComplete)
                    _winnerLabel.text = "Racing...";
                else
                    _winnerLabel.text = _raceSO.PlayerWon ? "Player Wins!" : "Bot Wins!";
            }
        }

        public ZoneControlZoneRaceSO RaceSO => _raceSO;
    }
}
