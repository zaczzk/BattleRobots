using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCapturePulseController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCapturePulseSO _pulseSO;
        [SerializeField] private PlayerWalletSO            _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPulse;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _chargeLabel;
        [SerializeField] private Text       _pulseCountLabel;
        [SerializeField] private Slider     _chargeBar;
        [SerializeField] private GameObject _panel;

        private Action _handleCaptureDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handlePulseDelegate;

        private void Awake()
        {
            _handleCaptureDelegate      = HandleCapture;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handlePulseDelegate        = HandlePulse;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleCaptureDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onPulse?.RegisterCallback(_handlePulseDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleCaptureDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPulse?.UnregisterCallback(_handlePulseDelegate);
        }

        private void HandleCapture()
        {
            if (_pulseSO == null) return;
            int prev = _pulseSO.PulseCount;
            _pulseSO.RecordCapture();
            if (_pulseSO.PulseCount > prev)
                _wallet?.AddFunds(_pulseSO.BonusPerPulse);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _pulseSO?.Reset();
            Refresh();
        }

        private void HandlePulse() => Refresh();

        public void Refresh()
        {
            if (_pulseSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_chargeLabel != null)
                _chargeLabel.text = $"Charge: {_pulseSO.ChargeCount}/{_pulseSO.PulseThreshold}";

            if (_pulseCountLabel != null)
                _pulseCountLabel.text = $"Pulses: {_pulseSO.PulseCount}";

            if (_chargeBar != null)
                _chargeBar.value = _pulseSO.ChargeProgress;
        }

        public ZoneControlCapturePulseSO PulseSO => _pulseSO;
    }
}
