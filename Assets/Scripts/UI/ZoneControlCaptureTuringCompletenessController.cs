using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureTuringCompletenessController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureTuringCompletenessSO _turingCompletenessSO;
        [SerializeField] private PlayerWallet                           _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onTuringCompletenessSimulated;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _simulationStepLabel;
        [SerializeField] private Text       _simulationLabel;
        [SerializeField] private Slider     _simulationStepBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleSimulationDelegate;

        private void Awake()
        {
            _handlePlayerDelegate      = HandlePlayerCaptured;
            _handleBotDelegate         = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleSimulationDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onTuringCompletenessSimulated?.RegisterCallback(_handleSimulationDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onTuringCompletenessSimulated?.UnregisterCallback(_handleSimulationDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_turingCompletenessSO == null) return;
            int bonus = _turingCompletenessSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_turingCompletenessSO == null) return;
            _turingCompletenessSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _turingCompletenessSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_turingCompletenessSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_simulationStepLabel != null)
                _simulationStepLabel.text = $"Simulation Steps: {_turingCompletenessSO.SimulationSteps}/{_turingCompletenessSO.SimulationStepsNeeded}";

            if (_simulationLabel != null)
                _simulationLabel.text = $"Simulations: {_turingCompletenessSO.SimulationCount}";

            if (_simulationStepBar != null)
                _simulationStepBar.value = _turingCompletenessSO.SimulationStepProgress;
        }

        public ZoneControlCaptureTuringCompletenessSO TuringCompletenessSO => _turingCompletenessSO;
    }
}
