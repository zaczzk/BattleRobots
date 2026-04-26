using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureLinearTemporalLogicController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureLinearTemporalLogicSO _linearTemporalLogicSO;
        [SerializeField] private PlayerWallet                             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onLinearTemporalLogicCompleted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _temporalFormulaLabel;
        [SerializeField] private Text       _formulaCountLabel;
        [SerializeField] private Slider     _temporalFormulaBar;
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
            _onLinearTemporalLogicCompleted?.RegisterCallback(_handleCompletedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onLinearTemporalLogicCompleted?.UnregisterCallback(_handleCompletedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_linearTemporalLogicSO == null) return;
            int bonus = _linearTemporalLogicSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_linearTemporalLogicSO == null) return;
            _linearTemporalLogicSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _linearTemporalLogicSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_linearTemporalLogicSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_temporalFormulaLabel != null)
                _temporalFormulaLabel.text =
                    $"Temporal Formulas: {_linearTemporalLogicSO.TemporalFormulas}/{_linearTemporalLogicSO.TemporalFormulasNeeded}";

            if (_formulaCountLabel != null)
                _formulaCountLabel.text = $"Formulas: {_linearTemporalLogicSO.FormulaCount}";

            if (_temporalFormulaBar != null)
                _temporalFormulaBar.value = _linearTemporalLogicSO.TemporalFormulaProgress;
        }

        public ZoneControlCaptureLinearTemporalLogicSO LinearTemporalLogicSO => _linearTemporalLogicSO;
    }
}
