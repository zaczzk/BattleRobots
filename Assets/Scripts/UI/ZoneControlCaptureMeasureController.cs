using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureMeasureController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureMeasureSO _measureSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMeasureIntegrated;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _sampleLabel;
        [SerializeField] private Text       _integrateLabel;
        [SerializeField] private Slider     _sampleBar;
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
            _onMeasureIntegrated?.RegisterCallback(_handleIntegratedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMeasureIntegrated?.UnregisterCallback(_handleIntegratedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_measureSO == null) return;
            int bonus = _measureSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_measureSO == null) return;
            _measureSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _measureSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_measureSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_sampleLabel != null)
                _sampleLabel.text = $"Samples: {_measureSO.Samples}/{_measureSO.SamplesNeeded}";

            if (_integrateLabel != null)
                _integrateLabel.text = $"Integrations: {_measureSO.IntegrationCount}";

            if (_sampleBar != null)
                _sampleBar.value = _measureSO.SampleProgress;
        }

        public ZoneControlCaptureMeasureSO MeasureSO => _measureSO;
    }
}
