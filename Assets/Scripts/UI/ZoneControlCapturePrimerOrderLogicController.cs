using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCapturePrimerOrderLogicController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCapturePrimerOrderLogicSO _primerOrderLogicSO;
        [SerializeField] private PlayerWallet                          _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onFirstOrderCompletenessAchieved;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _validFormulaLabel;
        [SerializeField] private Text       _completenessCountLabel;
        [SerializeField] private Slider     _validFormulaBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleAchievedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleAchievedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onFirstOrderCompletenessAchieved?.RegisterCallback(_handleAchievedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onFirstOrderCompletenessAchieved?.UnregisterCallback(_handleAchievedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_primerOrderLogicSO == null) return;
            int bonus = _primerOrderLogicSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_primerOrderLogicSO == null) return;
            _primerOrderLogicSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _primerOrderLogicSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_primerOrderLogicSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_validFormulaLabel != null)
                _validFormulaLabel.text =
                    $"Valid Formulas: {_primerOrderLogicSO.ValidFormulas}/{_primerOrderLogicSO.ValidFormulasNeeded}";

            if (_completenessCountLabel != null)
                _completenessCountLabel.text = $"Completeness: {_primerOrderLogicSO.CompletenessCount}";

            if (_validFormulaBar != null)
                _validFormulaBar.value = _primerOrderLogicSO.ValidFormulaProgress;
        }

        public ZoneControlCapturePrimerOrderLogicSO PrimerOrderLogicSO => _primerOrderLogicSO;
    }
}
