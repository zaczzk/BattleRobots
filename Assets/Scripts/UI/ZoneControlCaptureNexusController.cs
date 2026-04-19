using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureNexusController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureNexusSO _nexusSO;
        [SerializeField] private PlayerWalletSO            _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onNexusComplete;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _stepLabel;
        [SerializeField] private Text       _nexusCountLabel;
        [SerializeField] private Slider     _nexusProgressBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerCaptureDelegate;
        private Action _handleBotCaptureDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleNexusCompleteDelegate;

        private void Awake()
        {
            _handlePlayerCaptureDelegate = HandlePlayerCaptured;
            _handleBotCaptureDelegate    = HandleBotCaptured;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _handleNexusCompleteDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCaptureDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotCaptureDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onNexusComplete?.RegisterCallback(_handleNexusCompleteDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCaptureDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCaptureDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onNexusComplete?.UnregisterCallback(_handleNexusCompleteDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_nexusSO == null) return;
            int prev = _nexusSO.NexusCount;
            _nexusSO.RecordPlayerCapture();
            if (_nexusSO.NexusCount > prev)
                _wallet?.AddFunds(_nexusSO.BonusPerNexus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_nexusSO == null) return;
            _nexusSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _nexusSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_nexusSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_stepLabel != null)
                _stepLabel.text = $"Step: {_nexusSO.NexusStep}/3";

            if (_nexusCountLabel != null)
                _nexusCountLabel.text = $"Nexus: {_nexusSO.NexusCount}";

            if (_nexusProgressBar != null)
                _nexusProgressBar.value = _nexusSO.NexusProgress;
        }

        public ZoneControlCaptureNexusSO NexusSO => _nexusSO;
    }
}
