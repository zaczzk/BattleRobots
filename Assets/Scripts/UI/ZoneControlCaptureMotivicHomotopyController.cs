using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureMotivicHomotopyController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureMotivicHomotopySO _motivicHomotopySO;
        [SerializeField] private PlayerWallet                         _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMotivicHomotopyContracted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _a1LocalizationLabel;
        [SerializeField] private Text       _contractLabel;
        [SerializeField] private Slider     _a1LocalizationBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleContractedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleContractedDelegate   = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMotivicHomotopyContracted?.RegisterCallback(_handleContractedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMotivicHomotopyContracted?.UnregisterCallback(_handleContractedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_motivicHomotopySO == null) return;
            int bonus = _motivicHomotopySO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_motivicHomotopySO == null) return;
            _motivicHomotopySO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _motivicHomotopySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_motivicHomotopySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_a1LocalizationLabel != null)
                _a1LocalizationLabel.text = $"A1-Localizations: {_motivicHomotopySO.A1Localizations}/{_motivicHomotopySO.A1LocalizationsNeeded}";

            if (_contractLabel != null)
                _contractLabel.text = $"Contractions: {_motivicHomotopySO.ContractionCount}";

            if (_a1LocalizationBar != null)
                _a1LocalizationBar.value = _motivicHomotopySO.A1LocalizationProgress;
        }

        public ZoneControlCaptureMotivicHomotopySO MotivicHomotopySO => _motivicHomotopySO;
    }
}
