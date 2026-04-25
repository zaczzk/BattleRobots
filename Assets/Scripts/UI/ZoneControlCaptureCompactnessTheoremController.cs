using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCompactnessTheoremController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCompactnessTheoremSO _compactnessTheoremSO;
        [SerializeField] private PlayerWallet                             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCompactnessTheoremSatisfied;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _finiteWitnessLabel;
        [SerializeField] private Text       _satisfactionCountLabel;
        [SerializeField] private Slider     _finiteWitnessBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleSatisfiedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleSatisfiedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCompactnessTheoremSatisfied?.RegisterCallback(_handleSatisfiedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCompactnessTheoremSatisfied?.UnregisterCallback(_handleSatisfiedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_compactnessTheoremSO == null) return;
            int bonus = _compactnessTheoremSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_compactnessTheoremSO == null) return;
            _compactnessTheoremSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _compactnessTheoremSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_compactnessTheoremSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_finiteWitnessLabel != null)
                _finiteWitnessLabel.text =
                    $"Finite Witnesses: {_compactnessTheoremSO.FiniteWitnesses}/{_compactnessTheoremSO.FiniteWitnessesNeeded}";

            if (_satisfactionCountLabel != null)
                _satisfactionCountLabel.text = $"Satisfactions: {_compactnessTheoremSO.SatisfactionCount}";

            if (_finiteWitnessBar != null)
                _finiteWitnessBar.value = _compactnessTheoremSO.FiniteWitnessProgress;
        }

        public ZoneControlCaptureCompactnessTheoremSO CompactnessTheoremSO => _compactnessTheoremSO;
    }
}
