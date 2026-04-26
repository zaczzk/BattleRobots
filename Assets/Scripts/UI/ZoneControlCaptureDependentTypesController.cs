using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureDependentTypesController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureDependentTypesSO _dependentTypesSO;
        [SerializeField] private PlayerWallet                       _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onDependentTypesCompleted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _typeWitnessLabel;
        [SerializeField] private Text       _witnessCountLabel;
        [SerializeField] private Slider     _typeWitnessBar;
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
            _onDependentTypesCompleted?.RegisterCallback(_handleCompletedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onDependentTypesCompleted?.UnregisterCallback(_handleCompletedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_dependentTypesSO == null) return;
            int bonus = _dependentTypesSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_dependentTypesSO == null) return;
            _dependentTypesSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _dependentTypesSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_dependentTypesSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_typeWitnessLabel != null)
                _typeWitnessLabel.text =
                    $"Type Witnesses: {_dependentTypesSO.TypeWitnesses}/{_dependentTypesSO.TypeWitnessesNeeded}";

            if (_witnessCountLabel != null)
                _witnessCountLabel.text = $"Witnesses: {_dependentTypesSO.WitnessCount}";

            if (_typeWitnessBar != null)
                _typeWitnessBar.value = _dependentTypesSO.TypeWitnessProgress;
        }

        public ZoneControlCaptureDependentTypesSO DependentTypesSO => _dependentTypesSO;
    }
}
