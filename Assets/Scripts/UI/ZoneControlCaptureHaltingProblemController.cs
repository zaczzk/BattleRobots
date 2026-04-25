using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureHaltingProblemController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureHaltingProblemSO _haltingProblemSO;
        [SerializeField] private PlayerWallet                       _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onHaltingProblemDecided;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _computationStepLabel;
        [SerializeField] private Text       _decisionLabel;
        [SerializeField] private Slider     _computationStepBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleDecisionDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleDecisionDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onHaltingProblemDecided?.RegisterCallback(_handleDecisionDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onHaltingProblemDecided?.UnregisterCallback(_handleDecisionDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_haltingProblemSO == null) return;
            int bonus = _haltingProblemSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_haltingProblemSO == null) return;
            _haltingProblemSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _haltingProblemSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_haltingProblemSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_computationStepLabel != null)
                _computationStepLabel.text = $"Computation Steps: {_haltingProblemSO.ComputationSteps}/{_haltingProblemSO.ComputationStepsNeeded}";

            if (_decisionLabel != null)
                _decisionLabel.text = $"Decisions: {_haltingProblemSO.DecisionCount}";

            if (_computationStepBar != null)
                _computationStepBar.value = _haltingProblemSO.ComputationStepProgress;
        }

        public ZoneControlCaptureHaltingProblemSO HaltingProblemSO => _haltingProblemSO;
    }
}
