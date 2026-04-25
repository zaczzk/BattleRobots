using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureContinuumHypothesisController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureContinuumHypothesisSO _continuumSO;
        [SerializeField] private PlayerWallet                            _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCardinalClassified;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _cardinalWitnessLabel;
        [SerializeField] private Text       _cardinalClassLabel;
        [SerializeField] private Slider     _cardinalWitnessBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleCardinalDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleCardinalDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCardinalClassified?.RegisterCallback(_handleCardinalDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCardinalClassified?.UnregisterCallback(_handleCardinalDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_continuumSO == null) return;
            int bonus = _continuumSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_continuumSO == null) return;
            _continuumSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _continuumSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_continuumSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_cardinalWitnessLabel != null)
                _cardinalWitnessLabel.text = $"Cardinal Witnesses: {_continuumSO.CardinalWitnesses}/{_continuumSO.CardinalWitnessesNeeded}";

            if (_cardinalClassLabel != null)
                _cardinalClassLabel.text = $"Classifications: {_continuumSO.CardinalClassCount}";

            if (_cardinalWitnessBar != null)
                _cardinalWitnessBar.value = _continuumSO.CardinalWitnessProgress;
        }

        public ZoneControlCaptureContinuumHypothesisSO ContinuumSO => _continuumSO;
    }
}
