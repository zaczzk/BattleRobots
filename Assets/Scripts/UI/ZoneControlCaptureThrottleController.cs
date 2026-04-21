using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureThrottleController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureThrottleSO _throttleSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onThrottleOpened;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _positionLabel;
        [SerializeField] private Text       _openLabel;
        [SerializeField] private Slider     _positionBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleOpenedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleOpenedDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onThrottleOpened?.RegisterCallback(_handleOpenedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onThrottleOpened?.UnregisterCallback(_handleOpenedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_throttleSO == null) return;
            int bonus = _throttleSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_throttleSO == null) return;
            _throttleSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _throttleSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_throttleSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_positionLabel != null)
                _positionLabel.text = $"Positions: {_throttleSO.Positions}/{_throttleSO.PositionsNeeded}";

            if (_openLabel != null)
                _openLabel.text = $"Opens: {_throttleSO.OpenCount}";

            if (_positionBar != null)
                _positionBar.value = _throttleSO.PositionProgress;
        }

        public ZoneControlCaptureThrottleSO ThrottleSO => _throttleSO;
    }
}
