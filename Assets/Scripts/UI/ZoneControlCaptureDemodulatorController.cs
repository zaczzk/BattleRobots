using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureDemodulatorController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureDemodulatorSO _demodulatorSO;
        [SerializeField] private PlayerWallet                     _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onDemodulatorExtracted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _sampleLabel;
        [SerializeField] private Text       _extractionLabel;
        [SerializeField] private Slider     _sampleBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleExtractedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleExtractedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onDemodulatorExtracted?.RegisterCallback(_handleExtractedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onDemodulatorExtracted?.UnregisterCallback(_handleExtractedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_demodulatorSO == null) return;
            int bonus = _demodulatorSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_demodulatorSO == null) return;
            _demodulatorSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _demodulatorSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_demodulatorSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_sampleLabel != null)
                _sampleLabel.text = $"Samples: {_demodulatorSO.Samples}/{_demodulatorSO.SamplesNeeded}";

            if (_extractionLabel != null)
                _extractionLabel.text = $"Extractions: {_demodulatorSO.ExtractionCount}";

            if (_sampleBar != null)
                _sampleBar.value = _demodulatorSO.SampleProgress;
        }

        public ZoneControlCaptureDemodulatorSO DemodulatorSO => _demodulatorSO;
    }
}
