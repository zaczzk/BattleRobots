using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureLocalizationController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureLocalizationSO _localizationSO;
        [SerializeField] private PlayerWallet                      _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onLocalizationApplied;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _primeLabel;
        [SerializeField] private Text       _localLabel;
        [SerializeField] private Slider     _primeBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleAppliedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleAppliedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onLocalizationApplied?.RegisterCallback(_handleAppliedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onLocalizationApplied?.UnregisterCallback(_handleAppliedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_localizationSO == null) return;
            int bonus = _localizationSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_localizationSO == null) return;
            _localizationSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _localizationSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_localizationSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_primeLabel != null)
                _primeLabel.text = $"Primes: {_localizationSO.Primes}/{_localizationSO.PrimesNeeded}";

            if (_localLabel != null)
                _localLabel.text = $"Localizations: {_localizationSO.LocalizationCount}";

            if (_primeBar != null)
                _primeBar.value = _localizationSO.PrimeProgress;
        }

        public ZoneControlCaptureLocalizationSO LocalizationSO => _localizationSO;
    }
}
