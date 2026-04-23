using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureTerminalController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureTerminalSO _terminalSO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onTerminalReached;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _arrowLabel;
        [SerializeField] private Text       _terminalLabel;
        [SerializeField] private Slider     _arrowBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleReachedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleReachedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onTerminalReached?.RegisterCallback(_handleReachedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onTerminalReached?.UnregisterCallback(_handleReachedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_terminalSO == null) return;
            int bonus = _terminalSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_terminalSO == null) return;
            _terminalSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _terminalSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_terminalSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_arrowLabel != null)
                _arrowLabel.text = $"Arrows: {_terminalSO.Arrows}/{_terminalSO.ArrowsNeeded}";

            if (_terminalLabel != null)
                _terminalLabel.text = $"Terminals: {_terminalSO.TerminalCount}";

            if (_arrowBar != null)
                _arrowBar.value = _terminalSO.ArrowProgress;
        }

        public ZoneControlCaptureTerminalSO TerminalSO => _terminalSO;
    }
}
