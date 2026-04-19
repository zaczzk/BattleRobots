using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCapturePrismaticController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCapturePrismaticSO _prismaticSO;
        [SerializeField] private PlayerWalletSO                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPrismaticPulse;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _totalLabel;
        [SerializeField] private Text       _pulseCountLabel;
        [SerializeField] private Slider     _pulseProgressBar;
        [SerializeField] private GameObject _panel;

        private Action _handleCaptureDelegate;
        private Action _handleBotCaptureDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handlePulseDelegate;

        private void Awake()
        {
            _handleCaptureDelegate      = HandleCapture;
            _handleBotCaptureDelegate   = HandleBotCapture;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handlePulseDelegate        = HandlePulse;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleCaptureDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotCaptureDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onPrismaticPulse?.RegisterCallback(_handlePulseDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleCaptureDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCaptureDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPrismaticPulse?.UnregisterCallback(_handlePulseDelegate);
        }

        private void HandleCapture()
        {
            if (_prismaticSO == null) return;
            int prev = _prismaticSO.PulseCount;
            _prismaticSO.RecordCapture();
            if (_prismaticSO.PulseCount > prev)
                _wallet?.AddFunds(_prismaticSO.BonusPerPulse);
            Refresh();
        }

        private void HandleBotCapture()
        {
            if (_prismaticSO == null) return;
            int prev = _prismaticSO.PulseCount;
            _prismaticSO.RecordCapture();
            if (_prismaticSO.PulseCount > prev)
                _wallet?.AddFunds(_prismaticSO.BonusPerPulse);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _prismaticSO?.Reset();
            Refresh();
        }

        private void HandlePulse() => Refresh();

        public void Refresh()
        {
            if (_prismaticSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_totalLabel != null)
                _totalLabel.text = $"Total: {_prismaticSO.TotalCaptures}";

            if (_pulseCountLabel != null)
                _pulseCountLabel.text = $"Pulse: {_prismaticSO.PulseCount}";

            if (_pulseProgressBar != null)
                _pulseProgressBar.value = _prismaticSO.PulseProgress;
        }

        public ZoneControlCapturePrismaticSO PrismaticSO => _prismaticSO;
    }
}
