using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCapturePeriodDomainController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCapturePeriodDomainSO _periodDomainSO;
        [SerializeField] private PlayerWallet                     _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPeriodDomainPolarized;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _hodgeFiltrationLabel;
        [SerializeField] private Text       _polarizeLabel;
        [SerializeField] private Slider     _hodgeFiltrationBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handlePolarizedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handlePolarizedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onPeriodDomainPolarized?.RegisterCallback(_handlePolarizedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPeriodDomainPolarized?.UnregisterCallback(_handlePolarizedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_periodDomainSO == null) return;
            int bonus = _periodDomainSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_periodDomainSO == null) return;
            _periodDomainSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _periodDomainSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_periodDomainSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_hodgeFiltrationLabel != null)
                _hodgeFiltrationLabel.text = $"Hodge Filtrations: {_periodDomainSO.HodgeFiltrations}/{_periodDomainSO.HodgeFiltrationsNeeded}";

            if (_polarizeLabel != null)
                _polarizeLabel.text = $"Polarizations: {_periodDomainSO.PolarizationCount}";

            if (_hodgeFiltrationBar != null)
                _hodgeFiltrationBar.value = _periodDomainSO.HodgeFiltrationProgress;
        }

        public ZoneControlCapturePeriodDomainSO PeriodDomainSO => _periodDomainSO;
    }
}
