using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSequencerController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSequencerSO _sequencerSO;
        [SerializeField] private PlayerWallet                   _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSequencerAdvanced;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _stepLabel;
        [SerializeField] private Text       _sequenceLabel;
        [SerializeField] private Slider     _stepBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleSequencerDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleSequencerDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onSequencerAdvanced?.RegisterCallback(_handleSequencerDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSequencerAdvanced?.UnregisterCallback(_handleSequencerDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_sequencerSO == null) return;
            int bonus = _sequencerSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_sequencerSO == null) return;
            _sequencerSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _sequencerSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_sequencerSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_stepLabel != null)
                _stepLabel.text = $"Steps: {_sequencerSO.Steps}/{_sequencerSO.StepsNeeded}";

            if (_sequenceLabel != null)
                _sequenceLabel.text = $"Sequences: {_sequencerSO.SequenceCount}";

            if (_stepBar != null)
                _stepBar.value = _sequencerSO.StepProgress;
        }

        public ZoneControlCaptureSequencerSO SequencerSO => _sequencerSO;
    }
}
