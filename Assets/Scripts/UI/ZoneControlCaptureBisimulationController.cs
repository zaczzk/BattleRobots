using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureBisimulationController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureBisimulationSO _bisimulationSO;
        [SerializeField] private PlayerWallet                      _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onBisimulationCompleted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _bisimulationStepLabel;
        [SerializeField] private Text       _bisimulationCountLabel;
        [SerializeField] private Slider     _bisimulationStepBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleCompletedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleCompletedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onBisimulationCompleted?.RegisterCallback(_handleCompletedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onBisimulationCompleted?.UnregisterCallback(_handleCompletedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_bisimulationSO == null) return;
            int bonus = _bisimulationSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_bisimulationSO == null) return;
            _bisimulationSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _bisimulationSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_bisimulationSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_bisimulationStepLabel != null)
                _bisimulationStepLabel.text =
                    $"Bisimulation Steps: {_bisimulationSO.BisimulationSteps}/{_bisimulationSO.BisimulationStepsNeeded}";

            if (_bisimulationCountLabel != null)
                _bisimulationCountLabel.text = $"Bisimulations: {_bisimulationSO.BisimulationCount}";

            if (_bisimulationStepBar != null)
                _bisimulationStepBar.value = _bisimulationSO.BisimulationStepProgress;
        }

        public ZoneControlCaptureBisimulationSO BisimulationSO => _bisimulationSO;
    }
}
