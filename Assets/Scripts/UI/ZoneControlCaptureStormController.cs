using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureStormController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureStormSO _stormSO;
        [SerializeField] private PlayerWalletSO            _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onStormActivated;
        [SerializeField] private VoidGameEvent _onStormEnded;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Slider     _progressBar;
        [SerializeField] private GameObject _panel;

        private Action _handleCaptureDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleStormActivatedDelegate;
        private Action _handleStormEndedDelegate;

        private void Awake()
        {
            _handleCaptureDelegate       = HandleCapture;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _handleStormActivatedDelegate = Refresh;
            _handleStormEndedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleCaptureDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onStormActivated?.RegisterCallback(_handleStormActivatedDelegate);
            _onStormEnded?.RegisterCallback(_handleStormEndedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleCaptureDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onStormActivated?.UnregisterCallback(_handleStormActivatedDelegate);
            _onStormEnded?.UnregisterCallback(_handleStormEndedDelegate);
        }

        private void Update()
        {
            if (_stormSO != null && _stormSO.IsStormActive)
            {
                _stormSO.Tick(Time.deltaTime);
                Refresh();
            }
        }

        private void HandleCapture()
        {
            if (_stormSO == null) return;
            int bonus = _stormSO.RecordCapture();
            if (bonus > 0)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _stormSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_stormSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
            {
                _statusLabel.text = _stormSO.IsStormActive
                    ? "STORM ACTIVE!"
                    : $"Charging: {_stormSO.StormCharges}/{_stormSO.ChargesRequired}";
            }

            if (_progressBar != null)
                _progressBar.value = _stormSO.ChargeProgress;
        }

        public ZoneControlCaptureStormSO StormSO => _stormSO;
    }
}
