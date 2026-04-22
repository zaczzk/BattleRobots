using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureMultiplexerController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureMultiplexerSO _multiplexerSO;
        [SerializeField] private PlayerWallet                     _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMultiplexerRouted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _inputLabel;
        [SerializeField] private Text       _routeLabel;
        [SerializeField] private Slider     _inputBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleRoutedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleRoutedDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMultiplexerRouted?.RegisterCallback(_handleRoutedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMultiplexerRouted?.UnregisterCallback(_handleRoutedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_multiplexerSO == null) return;
            int bonus = _multiplexerSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_multiplexerSO == null) return;
            _multiplexerSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _multiplexerSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_multiplexerSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_inputLabel != null)
                _inputLabel.text = $"Inputs: {_multiplexerSO.Inputs}/{_multiplexerSO.InputsNeeded}";

            if (_routeLabel != null)
                _routeLabel.text = $"Routes: {_multiplexerSO.RouteCount}";

            if (_inputBar != null)
                _inputBar.value = _multiplexerSO.InputProgress;
        }

        public ZoneControlCaptureMultiplexerSO MultiplexerSO => _multiplexerSO;
    }
}
