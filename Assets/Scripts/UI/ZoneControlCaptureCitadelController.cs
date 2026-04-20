using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCitadelController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCitadelSO _citadelSO;
        [SerializeField] private PlayerWalletSO               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCitadelBuilt;
        [SerializeField] private VoidGameEvent _onCitadelDemolished;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Text       _citadelCountLabel;
        [SerializeField] private Slider     _buildBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleCitadelBuiltDelegate;
        private Action _handleCitadelDemolishedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate             = HandlePlayerCaptured;
            _handleBotDelegate                = HandleBotCaptured;
            _handleMatchStartedDelegate       = HandleMatchStarted;
            _handleCitadelBuiltDelegate       = Refresh;
            _handleCitadelDemolishedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCitadelBuilt?.RegisterCallback(_handleCitadelBuiltDelegate);
            _onCitadelDemolished?.RegisterCallback(_handleCitadelDemolishedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCitadelBuilt?.UnregisterCallback(_handleCitadelBuiltDelegate);
            _onCitadelDemolished?.UnregisterCallback(_handleCitadelDemolishedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_citadelSO == null) return;
            int bonus = _citadelSO.RecordPlayerCapture();
            if (bonus > 0)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_citadelSO == null) return;
            _citadelSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _citadelSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_citadelSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
            {
                _statusLabel.text = _citadelSO.IsCitadelBuilt
                    ? "CITADEL BUILT!"
                    : $"Building: {_citadelSO.BuildCount}/{_citadelSO.CapturesForCitadel}";
            }

            if (_citadelCountLabel != null)
                _citadelCountLabel.text = $"Citadels: {_citadelSO.CitadelCount}";

            if (_buildBar != null)
                _buildBar.value = _citadelSO.IsCitadelBuilt ? 1f : _citadelSO.BuildProgress;
        }

        public ZoneControlCaptureCitadelSO CitadelSO => _citadelSO;
    }
}
