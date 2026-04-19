using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCycloneController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCycloneSO _cycloneSO;
        [SerializeField] private PlayerWalletSO              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCycloneOpened;
        [SerializeField] private VoidGameEvent _onCycloneClosed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Text       _cycloneCountLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleCaptureDelegate;
        private Action _handleBotCaptureDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleCycloneOpenedDelegate;
        private Action _handleCycloneClosedDelegate;

        private void Awake()
        {
            _handleCaptureDelegate       = HandleCapture;
            _handleBotCaptureDelegate    = HandleBotCapture;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _handleCycloneOpenedDelegate = HandleCycloneOpened;
            _handleCycloneClosedDelegate = HandleCycloneClosed;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleCaptureDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotCaptureDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCycloneOpened?.RegisterCallback(_handleCycloneOpenedDelegate);
            _onCycloneClosed?.RegisterCallback(_handleCycloneClosedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleCaptureDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCaptureDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCycloneOpened?.UnregisterCallback(_handleCycloneOpenedDelegate);
            _onCycloneClosed?.UnregisterCallback(_handleCycloneClosedDelegate);
        }

        private void Update()
        {
            if (_cycloneSO == null || !_cycloneSO.IsActive) return;
            _cycloneSO.Tick(Time.deltaTime);
            Refresh();
        }

        private void HandleCapture()
        {
            if (_cycloneSO == null) return;
            bool wasActive = _cycloneSO.IsActive;
            int  bonus     = _cycloneSO.GetCaptureBonus();
            _cycloneSO.RecordCapture();
            if (wasActive && bonus > 0)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCapture()
        {
            if (_cycloneSO == null) return;
            _cycloneSO.RecordCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _cycloneSO?.Reset();
            Refresh();
        }

        private void HandleCycloneOpened() => Refresh();
        private void HandleCycloneClosed()  => Refresh();

        public void Refresh()
        {
            if (_cycloneSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
            {
                int remaining = _cycloneSO.ActivatesEveryN
                              - (_cycloneSO.TotalCaptures % _cycloneSO.ActivatesEveryN);
                _statusLabel.text = _cycloneSO.IsActive
                    ? "CYCLONE ACTIVE!"
                    : $"Next: {remaining} more";
            }

            if (_cycloneCountLabel != null)
                _cycloneCountLabel.text = $"Cyclones: {_cycloneSO.CycloneCount}";
        }

        public ZoneControlCaptureCycloneSO CycloneSO => _cycloneSO;
    }
}
