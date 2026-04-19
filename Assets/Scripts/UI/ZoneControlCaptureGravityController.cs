using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureGravityController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureGravitySO _gravitySO;
        [SerializeField] private PlayerWalletSO              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onGravityPeak;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _gravityLabel;
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Slider     _gravityBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleGravityPeakDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleGravityPeakDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onGravityPeak?.RegisterCallback(_handleGravityPeakDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onGravityPeak?.UnregisterCallback(_handleGravityPeakDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_gravitySO == null) return;
            int bonus = _gravitySO.RecordPlayerCapture();
            if (bonus > 0)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_gravitySO == null) return;
            _gravitySO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _gravitySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_gravitySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_gravityLabel != null)
                _gravityLabel.text = $"Gravity: {_gravitySO.GravityProgress * 100f:F0}%";

            if (_statusLabel != null)
                _statusLabel.text = _gravitySO.IsAtPeak ? "CRITICAL!" : "Building";

            if (_gravityBar != null)
                _gravityBar.value = _gravitySO.GravityProgress;
        }

        public ZoneControlCaptureGravitySO GravitySO => _gravitySO;
    }
}
