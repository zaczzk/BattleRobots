using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureBusController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureBusSO _busSO;
        [SerializeField] private PlayerWallet             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onBusTransmitted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _signalLabel;
        [SerializeField] private Text       _busLabel;
        [SerializeField] private Slider     _signalBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleTransmittedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleTransmittedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onBusTransmitted?.RegisterCallback(_handleTransmittedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onBusTransmitted?.UnregisterCallback(_handleTransmittedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_busSO == null) return;
            int bonus = _busSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_busSO == null) return;
            _busSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _busSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_busSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_signalLabel != null)
                _signalLabel.text = $"Signals: {_busSO.Signals}/{_busSO.SignalsNeeded}";

            if (_busLabel != null)
                _busLabel.text = $"Transmissions: {_busSO.TransmissionCount}";

            if (_signalBar != null)
                _signalBar.value = _busSO.SignalProgress;
        }

        public ZoneControlCaptureBusSO BusSO => _busSO;
    }
}
