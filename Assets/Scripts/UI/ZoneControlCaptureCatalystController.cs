using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCatalystController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCatalystSO _catalystSO;
        [SerializeField] private PlayerWalletSO               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCatalystActivated;
        [SerializeField] private VoidGameEvent _onCatalystExpired;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Text       _catalystCountLabel;
        [SerializeField] private Slider     _progressBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleActivatedDelegate;
        private Action _handleExpiredDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleActivatedDelegate    = HandleCatalystActivated;
            _handleExpiredDelegate      = HandleCatalystExpired;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCatalystActivated?.RegisterCallback(_handleActivatedDelegate);
            _onCatalystExpired?.RegisterCallback(_handleExpiredDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCatalystActivated?.UnregisterCallback(_handleActivatedDelegate);
            _onCatalystExpired?.UnregisterCallback(_handleExpiredDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_catalystSO == null) return;
            int bonus = _catalystSO.RecordPlayerCapture();
            if (bonus > 0)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_catalystSO == null) return;
            _catalystSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _catalystSO?.Reset();
            Refresh();
        }

        private void HandleCatalystActivated() => Refresh();
        private void HandleCatalystExpired()    => Refresh();

        public void Refresh()
        {
            if (_catalystSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
            {
                _statusLabel.text = _catalystSO.IsCatalystActive
                    ? $"CATALYST ACTIVE! {_catalystSO.CatalystCapturesRemaining} left"
                    : $"Charging: {_catalystSO.CapturesForActivation - (int)(_catalystSO.ActivationProgress * _catalystSO.CapturesForActivation)}/{_catalystSO.CapturesForActivation}";
            }

            if (_catalystCountLabel != null)
                _catalystCountLabel.text = $"Catalysts: {_catalystSO.CatalystCount}";

            if (_progressBar != null)
                _progressBar.value = _catalystSO.IsCatalystActive
                    ? Mathf.Clamp01(_catalystSO.CatalystCapturesRemaining / (float)_catalystSO.CatalystDurationCaptures)
                    : _catalystSO.ActivationProgress;
        }

        public ZoneControlCaptureCatalystSO CatalystSO => _catalystSO;
    }
}
