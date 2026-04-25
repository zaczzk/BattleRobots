using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureChoiceAxiomController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureChoiceAxiomSO _choiceAxiomSO;
        [SerializeField] private PlayerWallet                    _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onChoiceAxiomApplied;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _wellOrderingLabel;
        [SerializeField] private Text       _choiceFunctionCountLabel;
        [SerializeField] private Slider     _wellOrderingBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleChoiceDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleChoiceDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onChoiceAxiomApplied?.RegisterCallback(_handleChoiceDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onChoiceAxiomApplied?.UnregisterCallback(_handleChoiceDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_choiceAxiomSO == null) return;
            int bonus = _choiceAxiomSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_choiceAxiomSO == null) return;
            _choiceAxiomSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _choiceAxiomSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_choiceAxiomSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_wellOrderingLabel != null)
                _wellOrderingLabel.text =
                    $"Well-Orderings: {_choiceAxiomSO.WellOrderings}/{_choiceAxiomSO.WellOrderingsNeeded}";

            if (_choiceFunctionCountLabel != null)
                _choiceFunctionCountLabel.text = $"Choice Functions: {_choiceAxiomSO.ChoiceFunctionCount}";

            if (_wellOrderingBar != null)
                _wellOrderingBar.value = _choiceAxiomSO.WellOrderingProgress;
        }

        public ZoneControlCaptureChoiceAxiomSO ChoiceAxiomSO => _choiceAxiomSO;
    }
}
