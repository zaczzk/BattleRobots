using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureModulatorController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureModulatorSO _modulatorSO;
        [SerializeField] private PlayerWallet                   _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onModulatorModulated;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _signalLabel;
        [SerializeField] private Text       _modulationLabel;
        [SerializeField] private Slider     _signalBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleModulatorDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleModulatorDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onModulatorModulated?.RegisterCallback(_handleModulatorDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onModulatorModulated?.UnregisterCallback(_handleModulatorDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_modulatorSO == null) return;
            int bonus = _modulatorSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_modulatorSO == null) return;
            _modulatorSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _modulatorSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_modulatorSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_signalLabel != null)
                _signalLabel.text = $"Signals: {_modulatorSO.Signals}/{_modulatorSO.SignalsNeeded}";

            if (_modulationLabel != null)
                _modulationLabel.text = $"Modulations: {_modulatorSO.ModulationCount}";

            if (_signalBar != null)
                _signalBar.value = _modulatorSO.SignalProgress;
        }

        public ZoneControlCaptureModulatorSO ModulatorSO => _modulatorSO;
    }
}
