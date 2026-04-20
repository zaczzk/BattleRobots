using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureTempestController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureTempestSO _tempestSO;
        [SerializeField] private PlayerWalletSO               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onTempestOpened;
        [SerializeField] private VoidGameEvent _onTempestClosed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Text       _tempestCountLabel;
        [SerializeField] private Slider     _tempestBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleTempestOpenedDelegate;
        private Action _handleTempestClosedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate        = HandlePlayerCaptured;
            _handleBotDelegate           = HandleBotCaptured;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _handleTempestOpenedDelegate = Refresh;
            _handleTempestClosedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onTempestOpened?.RegisterCallback(_handleTempestOpenedDelegate);
            _onTempestClosed?.RegisterCallback(_handleTempestClosedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onTempestOpened?.UnregisterCallback(_handleTempestOpenedDelegate);
            _onTempestClosed?.UnregisterCallback(_handleTempestClosedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_tempestSO == null) return;
            int bonus = _tempestSO.RecordPlayerCapture();
            if (bonus > 0)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_tempestSO == null) return;
            _tempestSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _tempestSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_tempestSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
            {
                _statusLabel.text = _tempestSO.IsActive
                    ? $"TEMPEST! {_tempestSO.CapturesRemaining} left"
                    : $"Charging: {_tempestSO.BotCharge}/{_tempestSO.ChargeForTempest}";
            }

            if (_tempestCountLabel != null)
                _tempestCountLabel.text = $"Tempests: {_tempestSO.TempestCount}";

            if (_tempestBar != null)
                _tempestBar.value = _tempestSO.TempestProgress;
        }

        public ZoneControlCaptureTempestSO TempestSO => _tempestSO;
    }
}
