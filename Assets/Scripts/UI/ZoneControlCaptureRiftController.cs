using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureRiftController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureRiftSO _riftSO;
        [SerializeField] private PlayerWalletSO           _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onRiftOpened;
        [SerializeField] private VoidGameEvent _onRiftClosed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Text       _riftCountLabel;
        [SerializeField] private Slider     _idleProgressBar;
        [SerializeField] private GameObject _panel;

        private Action _handleCaptureDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleRiftOpenedDelegate;
        private Action _handleRiftClosedDelegate;

        private void Awake()
        {
            _handleCaptureDelegate      = HandleCapture;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleRiftOpenedDelegate   = Refresh;
            _handleRiftClosedDelegate   = Refresh;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleCaptureDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onRiftOpened?.RegisterCallback(_handleRiftOpenedDelegate);
            _onRiftClosed?.RegisterCallback(_handleRiftClosedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleCaptureDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onRiftOpened?.UnregisterCallback(_handleRiftOpenedDelegate);
            _onRiftClosed?.UnregisterCallback(_handleRiftClosedDelegate);
        }

        private void Update()
        {
            if (_riftSO != null && !_riftSO.IsRiftOpen)
            {
                _riftSO.Tick(Time.deltaTime);
                Refresh();
            }
        }

        private void HandleCapture()
        {
            if (_riftSO == null) return;
            int bonus = _riftSO.RecordCapture();
            if (bonus > 0)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _riftSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_riftSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
            {
                _statusLabel.text = _riftSO.IsRiftOpen
                    ? "RIFT OPEN!"
                    : $"Cooldown: {_riftSO.IdleTimer:F1}s";
            }

            if (_riftCountLabel != null)
                _riftCountLabel.text = $"Rifts: {_riftSO.RiftCount}";

            if (_idleProgressBar != null)
                _idleProgressBar.value = _riftSO.IdleProgress;
        }

        public ZoneControlCaptureRiftSO RiftSO => _riftSO;
    }
}
