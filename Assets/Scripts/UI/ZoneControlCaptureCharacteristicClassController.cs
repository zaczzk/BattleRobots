using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCharacteristicClassController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCharacteristicClassSO _characteristicClassSO;
        [SerializeField] private PlayerWallet                              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCharacteristicClassEvaluated;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _obstructionLabel;
        [SerializeField] private Text       _evaluateLabel;
        [SerializeField] private Slider     _obstructionBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleEvaluatedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleEvaluatedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCharacteristicClassEvaluated?.RegisterCallback(_handleEvaluatedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCharacteristicClassEvaluated?.UnregisterCallback(_handleEvaluatedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_characteristicClassSO == null) return;
            int bonus = _characteristicClassSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_characteristicClassSO == null) return;
            _characteristicClassSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _characteristicClassSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_characteristicClassSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_obstructionLabel != null)
                _obstructionLabel.text = $"Obstructions: {_characteristicClassSO.Obstructions}/{_characteristicClassSO.ObstructionsNeeded}";

            if (_evaluateLabel != null)
                _evaluateLabel.text = $"Evaluations: {_characteristicClassSO.EvaluationCount}";

            if (_obstructionBar != null)
                _obstructionBar.value = _characteristicClassSO.ObstructionProgress;
        }

        public ZoneControlCaptureCharacteristicClassSO CharacteristicClassSO => _characteristicClassSO;
    }
}
