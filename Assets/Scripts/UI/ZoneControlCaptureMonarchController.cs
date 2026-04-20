using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureMonarchController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureMonarchSO _monarchSO;
        [SerializeField] private PlayerWallet                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onThroneTaken;
        [SerializeField] private VoidGameEvent _onThroneToppled;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _throneLabel;
        [SerializeField] private Text       _turnsLabel;
        [SerializeField] private Slider     _buildBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleThroneTakenDelegate;
        private Action _handleThroneToppledDelegate;

        private void Awake()
        {
            _handlePlayerDelegate        = HandlePlayerCaptured;
            _handleBotDelegate           = HandleBotCaptured;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _handleThroneTakenDelegate   = Refresh;
            _handleThroneToppledDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onThroneTaken?.RegisterCallback(_handleThroneTakenDelegate);
            _onThroneToppled?.RegisterCallback(_handleThroneToppledDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onThroneTaken?.UnregisterCallback(_handleThroneTakenDelegate);
            _onThroneToppled?.UnregisterCallback(_handleThroneToppledDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_monarchSO == null) return;
            int bonus = _monarchSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_monarchSO == null) return;
            _monarchSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _monarchSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_monarchSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_throneLabel != null)
                _throneLabel.text = _monarchSO.IsOnThrone
                    ? "THRONE TAKEN!"
                    : $"Building: {_monarchSO.BuildCount}/{_monarchSO.CapturesForThrone}";

            if (_turnsLabel != null)
                _turnsLabel.text = $"Turns: {_monarchSO.TurnCount}";

            if (_buildBar != null)
                _buildBar.value = _monarchSO.BuildProgress;
        }

        public ZoneControlCaptureMonarchSO MonarchSO => _monarchSO;
    }
}
