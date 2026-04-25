using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCapturePeanoAxiomsController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCapturePeanoAxiomsSO _peanoAxiomsSO;
        [SerializeField] private PlayerWallet                     _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPeanoAxiomsConstructed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _successorConstructionLabel;
        [SerializeField] private Text       _axiomSetCountLabel;
        [SerializeField] private Slider     _successorConstructionBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleConstructedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleConstructedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onPeanoAxiomsConstructed?.RegisterCallback(_handleConstructedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPeanoAxiomsConstructed?.UnregisterCallback(_handleConstructedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_peanoAxiomsSO == null) return;
            int bonus = _peanoAxiomsSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_peanoAxiomsSO == null) return;
            _peanoAxiomsSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _peanoAxiomsSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_peanoAxiomsSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_successorConstructionLabel != null)
                _successorConstructionLabel.text =
                    $"Successor Constructions: {_peanoAxiomsSO.SuccessorConstructions}/{_peanoAxiomsSO.SuccessorConstructionsNeeded}";

            if (_axiomSetCountLabel != null)
                _axiomSetCountLabel.text = $"Axiom Sets: {_peanoAxiomsSO.AxiomSetCount}";

            if (_successorConstructionBar != null)
                _successorConstructionBar.value = _peanoAxiomsSO.SuccessorConstructionProgress;
        }

        public ZoneControlCapturePeanoAxiomsSO PeanoAxiomsSO => _peanoAxiomsSO;
    }
}
