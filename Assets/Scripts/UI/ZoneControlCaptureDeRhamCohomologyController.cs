using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureDeRhamCohomologyController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureDeRhamCohomologySO _deRhamSO;
        [SerializeField] private PlayerWallet                          _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onDeRhamCohomologyIntegrated;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _formLabel;
        [SerializeField] private Text       _integrateLabel;
        [SerializeField] private Slider     _formBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleIntegratedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleIntegratedDelegate   = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onDeRhamCohomologyIntegrated?.RegisterCallback(_handleIntegratedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onDeRhamCohomologyIntegrated?.UnregisterCallback(_handleIntegratedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_deRhamSO == null) return;
            int bonus = _deRhamSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_deRhamSO == null) return;
            _deRhamSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _deRhamSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_deRhamSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_formLabel != null)
                _formLabel.text = $"Forms: {_deRhamSO.Forms}/{_deRhamSO.FormsNeeded}";

            if (_integrateLabel != null)
                _integrateLabel.text = $"Integrations: {_deRhamSO.IntegrateCount}";

            if (_formBar != null)
                _formBar.value = _deRhamSO.FormProgress;
        }

        public ZoneControlCaptureDeRhamCohomologySO DeRhamSO => _deRhamSO;
    }
}
