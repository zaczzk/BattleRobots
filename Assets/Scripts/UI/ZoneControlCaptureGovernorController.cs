using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureGovernorController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureGovernorSO _governorSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onGovernorRegulated;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _flyweightLabel;
        [SerializeField] private Text       _regulationLabel;
        [SerializeField] private Slider     _flyweightBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleRegulatedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleRegulatedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onGovernorRegulated?.RegisterCallback(_handleRegulatedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onGovernorRegulated?.UnregisterCallback(_handleRegulatedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_governorSO == null) return;
            int bonus = _governorSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_governorSO == null) return;
            _governorSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _governorSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_governorSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_flyweightLabel != null)
                _flyweightLabel.text = $"Flyweights: {_governorSO.Flyweights}/{_governorSO.FlyweightsNeeded}";

            if (_regulationLabel != null)
                _regulationLabel.text = $"Regulations: {_governorSO.RegulationCount}";

            if (_flyweightBar != null)
                _flyweightBar.value = _governorSO.FlyweightProgress;
        }

        public ZoneControlCaptureGovernorSO GovernorSO => _governorSO;
    }
}
